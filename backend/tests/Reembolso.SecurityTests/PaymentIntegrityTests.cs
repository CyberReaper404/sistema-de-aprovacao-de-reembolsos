using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reembolso.Application.Dtos.Reimbursements;
using Reembolso.Domain.Enums;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.SecurityTests;

public sealed class PaymentIntegrityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentIntegrityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Pagamento_DeveFalharQuandoValorDivergirDoAprovado()
    {
        var reimbursementId = await CriarSolicitacaoAprovadaAsync();

        using var financeClient = _factory.CreateAuthenticatedClient("fernanda@empresa.test", "Senha@123");
        var response = await financeClient.PostAsJsonAsync(
            $"/api/reimbursements/{reimbursementId}/payment",
            new RecordPaymentRequest(PaymentMethod.BankTransfer, "PAG-VALOR-INCORRETO", DateTimeOffset.UtcNow, 89m, "Tentativa inválida"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = await dbContext.ReimbursementRequests.SingleAsync(x => x.Id == reimbursementId);

        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.False(await dbContext.PaymentRecords.AnyAsync(x => x.RequestId == reimbursementId));
    }

    [Fact]
    public async Task Pagamento_NaoDeveSerRegistradoDuasVezes()
    {
        var reimbursementId = await CriarSolicitacaoAprovadaAsync();

        using var financeClient = _factory.CreateAuthenticatedClient("fernanda@empresa.test", "Senha@123");
        var firstResponse = await financeClient.PostAsJsonAsync(
            $"/api/reimbursements/{reimbursementId}/payment",
            new RecordPaymentRequest(PaymentMethod.Pix, "PIX-UNICO-001", DateTimeOffset.UtcNow, 90m, "Primeiro pagamento"));
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        var secondResponse = await financeClient.PostAsJsonAsync(
            $"/api/reimbursements/{reimbursementId}/payment",
            new RecordPaymentRequest(PaymentMethod.Pix, "PIX-UNICO-002", DateTimeOffset.UtcNow, 90m, "Tentativa duplicada"));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = await dbContext.ReimbursementRequests.SingleAsync(x => x.Id == reimbursementId);
        var paymentCount = await dbContext.PaymentRecords.CountAsync(x => x.RequestId == reimbursementId);

        Assert.Equal(RequestStatus.Paid, request.Status);
        Assert.Equal(1, paymentCount);
    }

    private async Task<Guid> CriarSolicitacaoAprovadaAsync()
    {
        using var collaboratorClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var createResponse = await collaboratorClient.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            "Pagamento protegido",
            _factory.CategoryId,
            90m,
            "BRL",
            new DateOnly(2026, 4, 7),
            "Despesa pronta para pagamento"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);

        var submitResponse = await collaboratorClient.PostAsync($"/api/reimbursements/{created!.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var approveResponse = await managerClient.PostAsJsonAsync(
            $"/api/reimbursements/{created.Id}/approve",
            new ApproveReimbursementRequest(null, "Aprovado para pagamento"));
        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        return created.Id;
    }

    private sealed record ReimbursementDetailDto(Guid Id);
}
