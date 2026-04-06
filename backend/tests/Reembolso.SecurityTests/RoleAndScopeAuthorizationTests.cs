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
    public async Task Gestor_ForaDoEscopo_NaoDeveConsultarSolicitacao()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("debora@empresa.test", "Senha@123");
        var createResponse = await collaboratorClient.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            "Solicitação externa",
            _factory.CategoryId,
            80m,
            "BRL",
            new DateOnly(2026, 4, 15),
            "Despesa do outro centro"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var detailResponse = await managerClient.GetAsync($"/api/reimbursements/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, detailResponse.StatusCode);
    }

    [Fact]
    public async Task Gestor_NaoDeveRegistrarPagamento()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var createResponse = await collaboratorClient.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            "Pagamento indevido",
            _factory.CategoryId,
            90m,
            "BRL",
            new DateOnly(2026, 4, 16),
            "Teste de bloqueio por papel"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);

        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created!.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var approveResponse = await managerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/approve",
            new ApproveReimbursementRequest("Aprovado"));
        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        var paymentResponse = await managerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/payment",
            new RecordPaymentRequest(PaymentMethod.Pix, "PIX-001", DateTimeOffset.UtcNow, 90m, "Tentativa indevida"));

        Assert.Equal(HttpStatusCode.Forbidden, paymentResponse.StatusCode);
    }

    private sealed record ReimbursementDetailDto(Guid Id, RequestStatus Status);
}
