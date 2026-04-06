using System.Net;
using System.Net.Http.Json;
using Reembolso.Application.Common;
using Reembolso.Application.Dtos.Dashboard;
using Reembolso.Application.Dtos.Payments;
using Reembolso.Application.Dtos.Reimbursements;
using Reembolso.Domain.Enums;

namespace Reembolso.IntegrationTests;

public sealed class DashboardAndPaymentsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DashboardAndPaymentsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Financeiro_DeveListarEConsultarPagamentoRegistrado()
    {
        var reimbursementId = await CriarAprovarEPagarSolicitacaoAsync(
            "alice@empresa.test",
            new DateOnly(2026, 4, 10),
            90m,
            "Pagamento hotel",
            "HOTEL-001");

        using var financeClient = _factory.CreateAuthenticatedClient("fernanda@empresa.test", "Senha@123");

        var listHttpResponse = await financeClient.GetAsync("/api/payments?page=1&pageSize=20");
        if (listHttpResponse.StatusCode != HttpStatusCode.OK)
        {
            var body = await listHttpResponse.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Falha ao listar pagamentos: {(int)listHttpResponse.StatusCode} - {body}");
        }

        var listResponse = await listHttpResponse.Content.ReadFromJsonAsync<PagedResult<PaymentListItemResponse>>();
        Assert.NotNull(listResponse);
        var payment = Assert.Single(listResponse!.Items.Where(x => x.RequestId == reimbursementId));
        Assert.Equal("HOTEL-001", payment.PaymentReference);

        var detailHttpResponse = await financeClient.GetAsync($"/api/payments/{payment.Id}");
        if (detailHttpResponse.StatusCode != HttpStatusCode.OK)
        {
            var body = await detailHttpResponse.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Falha ao consultar pagamento: {(int)detailHttpResponse.StatusCode} - {body}");
        }

        var detail = await detailHttpResponse.Content.ReadFromJsonAsync<PaymentDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(reimbursementId, detail!.RequestId);
        Assert.Equal(90m, detail.AmountPaid);
    }

    [Fact]
    public async Task Colaborador_NaoDeveConsultarListaDePagamentos()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");

        var response = await collaboratorClient.GetAsync("/api/payments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DashboardPorPeriodo_DeveAgruparSolicitacoesPorMes()
    {
        await CriarAprovarEPagarSolicitacaoAsync(
            "alice@empresa.test",
            new DateOnly(2026, 4, 5),
            90m,
            "Taxi abril",
            "TX-001");

        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var createMayResponse = await collaboratorClient.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            "Almoco maio",
            _factory.CategoryId,
            60m,
            "BRL",
            new DateOnly(2026, 5, 12),
            "Refeicao externa"));
        createMayResponse.EnsureSuccessStatusCode();
        var mayRequest = await createMayResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(mayRequest);
        var submitMayResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{mayRequest!.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitMayResponse.StatusCode);

        using var financeClient = _factory.CreateAuthenticatedClient("fernanda@empresa.test", "Senha@123");
        var dashboardItems = await financeClient.GetFromJsonAsync<IReadOnlyCollection<DashboardByPeriodItemResponse>>(
            "/api/dashboard/by-period?from=2026-04-01&to=2026-05-31&groupBy=month");

        Assert.NotNull(dashboardItems);
        Assert.Equal(2, dashboardItems!.Count);

        var april = dashboardItems.Single(x => x.PeriodStart == new DateOnly(2026, 4, 1));
        Assert.Equal(1, april.TotalRequests);
        Assert.Equal(90m, april.TotalAmount);
        Assert.Equal(1, april.PaidRequests);
        Assert.Equal(90m, april.PaidAmount);

        var may = dashboardItems.Single(x => x.PeriodStart == new DateOnly(2026, 5, 1));
        Assert.Equal(1, may.TotalRequests);
        Assert.Equal(60m, may.TotalAmount);
        Assert.Equal(0, may.PaidRequests);
        Assert.Equal(0m, may.PaidAmount);
    }

    private async Task<Guid> CriarAprovarEPagarSolicitacaoAsync(
        string collaboratorEmail,
        DateOnly expenseDate,
        decimal amount,
        string title,
        string paymentReference)
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient(collaboratorEmail, "Senha@123");

        var createResponse = await collaboratorClient.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            title,
            _factory.CategoryId,
            amount,
            "BRL",
            expenseDate,
            "Despesa operacional"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);

        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created!.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var approveResponse = await managerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/approve",
            new ApproveReimbursementRequest("Aprovado para pagamento"));
        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        using var financeClient = _factory.CreateAuthenticatedClient("fernanda@empresa.test", "Senha@123");
        var paymentResponse = await financeClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/payment",
            new RecordPaymentRequest(PaymentMethod.BankTransfer, paymentReference, DateTimeOffset.UtcNow, amount, "Pagamento efetuado"));
        if (paymentResponse.StatusCode != HttpStatusCode.NoContent)
        {
            var body = await paymentResponse.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Falha ao registrar pagamento: {(int)paymentResponse.StatusCode} - {body}");
        }

        return created.Id;
    }

    private sealed record ReimbursementDetailDto(Guid Id);
}
