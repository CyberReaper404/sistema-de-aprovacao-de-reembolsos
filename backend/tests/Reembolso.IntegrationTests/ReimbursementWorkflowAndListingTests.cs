using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Reembolso.Application.Common;
using Reembolso.Application.Dtos.Reimbursements;
using Reembolso.Domain.Enums;

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

        var created = await CriarSolicitacaoAsync(collaboratorClient, "Táxi aeroporto", 80m, new DateOnly(2026, 4, 11), "Despesa inicial");
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
                new DateOnly(2026, 4, 11),
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

        var created = await CriarSolicitacaoAsync(collaboratorClient, "Hotel sem comprovante", 150m, new DateOnly(2026, 4, 12), "Valor acima do limite de comprovante");

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

        var primeira = await CriarSolicitacaoAsync(collaboratorClient, "Táxi centro", 30m, new DateOnly(2026, 4, 10), "Deslocamento curto");
        var segunda = await CriarSolicitacaoAsync(collaboratorClient, "Almoço cliente", 70m, new DateOnly(2026, 4, 11), "Reunião comercial");
        var terceira = await CriarSolicitacaoAsync(collaboratorClient, "Hotel viagem", 90m, new DateOnly(2026, 4, 12), "Viagem de trabalho");

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

        var created = await CriarSolicitacaoAsync(collaboratorClient, "Viagem fora do escopo", 90m, new DateOnly(2026, 4, 13), "Despesa do outro centro");
        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var approveResponse = await managerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/approve",
            new ApproveReimbursementRequest("Tentativa sem escopo"));

        Assert.Equal(HttpStatusCode.Forbidden, approveResponse.StatusCode);
        var body = await approveResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"forbidden\"", body);
    }

    [Fact]
    public async Task Upload_DeveRejeitarTipoNaoPermitido()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var created = await CriarSolicitacaoAsync(collaboratorClient, "Arquivo inválido", 80m, new DateOnly(2026, 4, 14), "Teste de tipo de arquivo");

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
        var created = await CriarSolicitacaoAsync(collaboratorClient, "Arquivo grande", 80m, new DateOnly(2026, 4, 14), "Teste de tamanho de arquivo");

        using var form = new MultipartFormDataContent();
        var oversizedContent = new ByteArrayContent(new byte[10 * 1024 * 1024 + 1]);
        oversizedContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(oversizedContent, "file", "comprovante.pdf");

        var response = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/attachments", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);
        return created!;
    }

    private sealed record ReimbursementDetailDto(Guid Id, string RequestNumber, string RowVersion);
}
