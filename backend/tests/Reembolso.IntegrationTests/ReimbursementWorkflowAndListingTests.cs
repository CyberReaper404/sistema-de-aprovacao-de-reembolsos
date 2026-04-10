using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reembolso.Application.Common;
using Reembolso.Application.Dtos.Reimbursements;
using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.IntegrationTests;

public sealed class ReimbursementWorkflowAndListingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReimbursementWorkflowAndListingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SolicitacaoEnviada_NaoDevePermitirEdicao()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");

        var created = await CriarSolicitacaoAsync(collaboratorClient, "Táxi aeroporto", 80m, new DateOnly(2026, 4, 3), "Despesa inicial");
        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        var submitted = await collaboratorClient.GetFromJsonAsync<ReimbursementDetailDto>($"/api/reimbursements/{created.Id}");
        Assert.NotNull(submitted);

        var updateResponse = await collaboratorClient.PutAsJsonAsync(
            $"/api/reimbursements/{created.Id}",
            new UpdateReimbursementDraftRequest(
                "Táxi aeroporto atualizado",
                _factory.CategoryId,
                85m,
                "BRL",
                new DateOnly(2026, 4, 3),
                "Tentativa de edição após envio",
                submitted!.RowVersion));

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
        var body = await updateResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"invalid_workflow_transition\"", body);
    }

    [Fact]
    public async Task Envio_DeveFalharQuandoComprovanteForObrigatorio()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");

        var created = await CriarSolicitacaoAsync(collaboratorClient, "Hotel sem comprovante", 150m, new DateOnly(2026, 4, 4), "Valor acima do limite de comprovante");

        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);

        Assert.Equal(HttpStatusCode.BadRequest, submitResponse.StatusCode);
        var body = await submitResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"validation_error\"", body);
        Assert.Contains("\"attachments\":", body);
    }

    [Fact]
    public async Task Listagem_DeveAplicarFiltrosPaginacaoEOrdenacao()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");

        var primeira = await CriarSolicitacaoAsync(collaboratorClient, "Táxi centro", 30m, new DateOnly(2026, 4, 2), "Deslocamento curto");
        var segunda = await CriarSolicitacaoAsync(collaboratorClient, "Almoço cliente", 70m, new DateOnly(2026, 4, 3), "Reunião comercial");
        var terceira = await CriarSolicitacaoAsync(collaboratorClient, "Hotel viagem", 90m, new DateOnly(2026, 4, 4), "Viagem de trabalho");

        var listHttpResponse = await collaboratorClient.GetAsync(
            "/api/reimbursements?page=1&pageSize=2&status=Draft&createdByMe=true&sort=amount:asc");
        if (listHttpResponse.StatusCode != HttpStatusCode.OK)
        {
            var body = await listHttpResponse.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Falha ao listar solicitações: {(int)listHttpResponse.StatusCode} - {body}");
        }

        var listResponse = await listHttpResponse.Content.ReadFromJsonAsync<PagedResult<ReimbursementListItemResponse>>();

        Assert.NotNull(listResponse);
        Assert.Equal(1, listResponse!.Page);
        Assert.Equal(2, listResponse.PageSize);
        Assert.True(listResponse.TotalItems >= 3);
        Assert.True(listResponse.TotalPages >= 2);
        Assert.Equal(2, listResponse.Items.Count);

        var orderedItems = listResponse.Items.ToArray();
        Assert.Equal(primeira.Id, orderedItems[0].Id);
        Assert.Equal(segunda.Id, orderedItems[1].Id);

        var filteredHttpResponse = await collaboratorClient.GetAsync(
            $"/api/reimbursements?page=1&pageSize=20&requestNumber={terceira.RequestNumber}");
        if (filteredHttpResponse.StatusCode != HttpStatusCode.OK)
        {
            var body = await filteredHttpResponse.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Falha ao filtrar solicitações: {(int)filteredHttpResponse.StatusCode} - {body}");
        }

        var filteredResponse = await filteredHttpResponse.Content.ReadFromJsonAsync<PagedResult<ReimbursementListItemResponse>>();

        Assert.NotNull(filteredResponse);
        var filteredItem = Assert.Single(filteredResponse!.Items);
        Assert.Equal(terceira.Id, filteredItem.Id);
    }

    [Fact]
    public async Task Gestor_ForaDoEscopo_NaoDeveAprovarSolicitacao()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("debora@empresa.test", "Senha@123");

        var created = await CriarSolicitacaoAsync(collaboratorClient, "Viagem fora do escopo", 90m, new DateOnly(2026, 4, 5), "Despesa do outro centro");
        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var approveResponse = await managerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/approve",
            new ApproveReimbursementRequest(null, "Tentativa sem escopo"));

        Assert.Equal(HttpStatusCode.Forbidden, approveResponse.StatusCode);
        var body = await approveResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"forbidden\"", body);
    }

    [Fact]
    public async Task Upload_DeveRejeitarTipoNaoPermitido()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var created = await CriarSolicitacaoAsync(collaboratorClient, "Arquivo inválido", 80m, new DateOnly(2026, 4, 6), "Teste de tipo de arquivo");

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("conteúdo inválido"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "file", "comprovante.txt");

        var response = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/attachments", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"contentType\":", body);
    }

    [Fact]
    public async Task Upload_DeveRejeitarArquivoAcimaDoLimite()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var created = await CriarSolicitacaoAsync(collaboratorClient, "Arquivo grande", 80m, new DateOnly(2026, 4, 6), "Teste de tamanho de arquivo");

        using var form = new MultipartFormDataContent();
        var oversizedContent = new ByteArrayContent(new byte[10 * 1024 * 1024 + 1]);
        oversizedContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(oversizedContent, "file", "comprovante.pdf");

        var response = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/attachments", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Colaborador_DeveListarApenasCategoriasAtivas()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.ReimbursementCategories.Add(
                new ReimbursementCategory("Categoria inativa", "Não deve aparecer na listagem pública", 100, 50, false, 30, DateTimeOffset.UtcNow));
            await dbContext.SaveChangesAsync();

            var inactiveCategory = await dbContext.ReimbursementCategories.SingleAsync(x => x.Name == "Categoria inativa");
            inactiveCategory.Update(
                inactiveCategory.Name,
                inactiveCategory.Description,
                inactiveCategory.MaxAmount,
                inactiveCategory.ReceiptRequiredAboveAmount,
                inactiveCategory.ReceiptRequiredAlways,
                inactiveCategory.SubmissionDeadlineDays,
                false,
                DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");

        var response = await collaboratorClient.GetAsync("/api/reimbursements/categories");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<ReimbursementCategoryOptionResponse>>();

        Assert.NotNull(payload);
        Assert.Contains(payload!, x => x.Id == _factory.CategoryId);
        Assert.DoesNotContain(payload!, x => x.Name == "Categoria inativa");
    }

    [Fact]
    public async Task Gestor_DeveSolicitarComplementacao_ComMotivoPadronizado()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var created = await CriarSolicitacaoAsync(collaboratorClient, "Táxi sem contexto", 90m, new DateOnly(2026, 4, 7), "Despesa");
        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var complementationResponse = await managerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/complementation",
            new RequestComplementationRequest(DecisionReasonCode.NeedMoreDetails, null));

        Assert.Equal(HttpStatusCode.NoContent, complementationResponse.StatusCode);

        var detail = await collaboratorClient.GetFromJsonAsync<ReimbursementDetailWithComplementationDto>($"/api/reimbursements/{created.Id}");
        Assert.NotNull(detail);
        Assert.True(detail!.HasPendingComplementation);
        Assert.Equal(DecisionReasonCode.NeedMoreDetails, detail.DecisionReasonCode);
    }

    [Fact]
    public async Task Colaborador_DevePoderEditarEReenviar_ComplementacaoPendente()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var created = await CriarSolicitacaoAsync(collaboratorClient, "Táxi para reunião", 90m, new DateOnly(2026, 4, 8), "Descrição inicial");
        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var complementationResponse = await managerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/complementation",
            new RequestComplementationRequest(DecisionReasonCode.NeedAdditionalDocument, "Anexar documento complementar."));
        Assert.Equal(HttpStatusCode.NoContent, complementationResponse.StatusCode);

        var submitted = await collaboratorClient.GetFromJsonAsync<ReimbursementDetailWithComplementationDto>($"/api/reimbursements/{created.Id}");
        Assert.NotNull(submitted);

        var updateResponse = await collaboratorClient.PutAsJsonAsync(
            $"/api/reimbursements/{created.Id}",
            new UpdateReimbursementDraftRequest(
                "Táxi para reunião externa",
                _factory.CategoryId,
                90m,
                "BRL",
                new DateOnly(2026, 4, 8),
                "Descrição complementar enviada.",
                submitted!.RowVersion));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var resubmitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, resubmitResponse.StatusCode);

        var finalDetail = await collaboratorClient.GetFromJsonAsync<ReimbursementDetailWithComplementationDto>($"/api/reimbursements/{created.Id}");
        Assert.NotNull(finalDetail);
        Assert.False(finalDetail!.HasPendingComplementation);
        Assert.Null(finalDetail.DecisionReasonCode);
    }

    private async Task<ReimbursementDetailDto> CriarSolicitacaoAsync(HttpClient client, string title, decimal amount, DateOnly expenseDate, string description)
    {
        var response = await client.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            title,
            _factory.CategoryId,
            amount,
            "BRL",
            expenseDate,
            description));

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Falha ao criar solicitação: {(int)response.StatusCode} - {body}");
        }

        var created = await response.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);
        return created!;
    }

    private sealed record ReimbursementDetailDto(Guid Id, string RequestNumber, string RowVersion);
    private sealed record ReimbursementDetailWithComplementationDto(Guid Id, string RowVersion, bool HasPendingComplementation, DecisionReasonCode? DecisionReasonCode);
}
