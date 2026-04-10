using System.Net;
using System.Net.Http.Json;
using Reembolso.Application.Dtos.Reimbursements;
using Reembolso.Domain.Enums;

namespace Reembolso.SecurityTests;

public sealed class RoleAndScopeAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoleAndScopeAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Colaborador_NaoDeveAprovarNemRecusarSolicitacao()
    {
        using var ownerClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var created = await CriarSolicitacaoAsync(ownerClient, "Solicitação para aprovação", 90m, new DateOnly(2026, 4, 7), "Despesa operacional");

        var submitResponse = await ownerClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        var approveResponse = await ownerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/approve",
            new ApproveReimbursementRequest(null, "Tentativa indevida"));

        var rejectResponse = await ownerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/reject",
            new RejectReimbursementRequest(DecisionReasonCode.Other, "Tentativa indevida"));

        Assert.Equal(HttpStatusCode.Forbidden, approveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, rejectResponse.StatusCode);

        var detail = await ownerClient.GetFromJsonAsync<ReimbursementDetailDto>($"/api/reimbursements/{created.Id}");
        Assert.NotNull(detail);
        Assert.Equal(RequestStatus.Submitted, detail!.Status);
    }

    [Fact]
    public async Task Financeiro_NaoDeveEditarNemEnviarDraft()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var created = await CriarSolicitacaoAsync(collaboratorClient, "Rascunho protegido", 70m, new DateOnly(2026, 4, 6), "Despesa em rascunho");

        using var financeClient = _factory.CreateAuthenticatedClient("fernanda@empresa.test", "Senha@123");
        var updateResponse = await financeClient.PutAsJsonAsync(
            $"/api/reimbursements/{created.Id}",
            new UpdateReimbursementDraftRequest(
                "Rascunho alterado",
                _factory.CategoryId,
                75m,
                "BRL",
                new DateOnly(2026, 4, 6),
                "Tentativa de edição indevida",
                created.RowVersion));

        var submitResponse = await financeClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, submitResponse.StatusCode);

        var detail = await collaboratorClient.GetFromJsonAsync<ReimbursementDetailDto>($"/api/reimbursements/{created.Id}");
        Assert.NotNull(detail);
        Assert.Equal(RequestStatus.Draft, detail!.Status);
        Assert.Equal("Rascunho protegido", detail.Title);
    }

    [Fact]
    public async Task Financeiro_NaoDeveAprovarNemRecusarSolicitacao()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var created = await CriarSolicitacaoAsync(collaboratorClient, "Solicitação enviada", 90m, new DateOnly(2026, 4, 5), "Despesa aguardando decisão");
        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        using var financeClient = _factory.CreateAuthenticatedClient("fernanda@empresa.test", "Senha@123");
        var approveResponse = await financeClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/approve",
            new ApproveReimbursementRequest(null, "Tentativa indevida"));

        var rejectResponse = await financeClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/reject",
            new RejectReimbursementRequest(DecisionReasonCode.Other, "Tentativa indevida"));

        Assert.Equal(HttpStatusCode.Forbidden, approveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, rejectResponse.StatusCode);

        var detail = await collaboratorClient.GetFromJsonAsync<ReimbursementDetailDto>($"/api/reimbursements/{created.Id}");
        Assert.NotNull(detail);
        Assert.Equal(RequestStatus.Submitted, detail!.Status);
    }

    [Fact]
    public async Task PapelNaoAdministrador_NaoDeveAcessarEndpointAdministrativoCritico()
    {
        using var financeClient = _factory.CreateAuthenticatedClient("fernanda@empresa.test", "Senha@123");

        var response = await financeClient.GetAsync("/api/admin/audit-entries?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"forbidden\"", body);
        Assert.Contains("\"traceId\":", body);
    }

    [Fact]
    public async Task Gestor_ForaDoEscopo_NaoDeveConsultarListaFiltradaDeOutroCentroDeCusto()
    {
        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");

        var response = await managerClient.GetAsync($"/api/reimbursements?costCenterId={_factory.SecondaryCostCenterId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Gestor_ForaDoEscopo_NaoDeveConsultarSolicitacao()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("debora@empresa.test", "Senha@123");
        var created = await CriarSolicitacaoAsync(collaboratorClient, "Solicitação externa", 80m, new DateOnly(2026, 4, 7), "Despesa do outro centro");

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var detailResponse = await managerClient.GetAsync($"/api/reimbursements/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, detailResponse.StatusCode);
    }

    [Fact]
    public async Task Gestor_NaoDeveRegistrarPagamento()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var created = await CriarSolicitacaoAsync(collaboratorClient, "Pagamento indevido", 90m, new DateOnly(2026, 4, 8), "Teste de bloqueio por papel");
        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var approveResponse = await managerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/approve",
            new ApproveReimbursementRequest(null, "Aprovado"));
        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        var paymentResponse = await managerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/payment",
            new RecordPaymentRequest(PaymentMethod.Pix, "PIX-001", DateTimeOffset.UtcNow, 90m, "Tentativa indevida"));

        Assert.Equal(HttpStatusCode.Forbidden, paymentResponse.StatusCode);

        var detail = await collaboratorClient.GetFromJsonAsync<ReimbursementDetailDto>($"/api/reimbursements/{created.Id}");
        Assert.NotNull(detail);
        Assert.Equal(RequestStatus.Approved, detail!.Status);
    }

    private async Task<ReimbursementDetailDto> CriarSolicitacaoAsync(HttpClient client, string title, decimal amount, DateOnly expenseDate, string description)
    {
        var createResponse = await client.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            title,
            _factory.CategoryId,
            amount,
            "BRL",
            expenseDate,
            description));
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var body = await createResponse.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Falha ao criar solicitação: {(int)createResponse.StatusCode} - {body}");
        }

        var created = await createResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);
        return created!;
    }

    private sealed record ReimbursementDetailDto(Guid Id, RequestStatus Status, string Title, string RowVersion);
}
