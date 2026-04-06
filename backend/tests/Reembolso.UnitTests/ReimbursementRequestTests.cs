using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;
using Reembolso.Domain.Exceptions;

namespace Reembolso.UnitTests;

public sealed class ReimbursementRequestTests
{
    [Fact]
    public void Submit_DeveAlterarStatusDeDraftParaSubmitted()
    {
        var request = CriarDraft();

        request.Submit(DateTimeOffset.UtcNow);

        Assert.Equal(RequestStatus.Submitted, request.Status);
        Assert.NotNull(request.SubmittedAt);
    }

    [Fact]
    public void UpdateDraft_DeveFalharQuandoSolicitacaoNaoEstaEmDraft()
    {
        var request = CriarDraft();
        request.Submit(DateTimeOffset.UtcNow);

        var action = () => request.UpdateDraft(
            "Novo título",
            Guid.NewGuid(),
            90m,
            "USD",
            new DateOnly(2026, 4, 5),
            "Nova descrição",
            DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DomainRuleException>(action);
        Assert.Equal("Somente rascunhos podem ser editados.", exception.Message);
    }

    [Fact]
    public void Submit_DeveFalharQuandoSolicitacaoJaFoiEnviada()
    {
        var request = CriarDraft();
        request.Submit(DateTimeOffset.UtcNow);

        var action = () => request.Submit(DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DomainRuleException>(action);
        Assert.Equal("Somente rascunhos podem ser enviados.", exception.Message);
    }

    [Fact]
    public void Approve_DeveAlterarStatusDeSubmittedParaApproved()
    {
        var request = CriarDraft();
        request.Submit(DateTimeOffset.UtcNow);

        request.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.NotNull(request.ApprovedAt);
        Assert.NotNull(request.ApprovedByUserId);
    }

    [Fact]
    public void Reject_DeveAlterarStatusDeSubmittedParaRejected()
    {
        var request = CriarDraft();
        request.Submit(DateTimeOffset.UtcNow);

        request.Reject(Guid.NewGuid(), "Despesa fora da política", DateTimeOffset.UtcNow);

        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Equal("Despesa fora da política", request.RejectionReason);
        Assert.NotNull(request.RejectedAt);
    }

    [Fact]
    public void RegisterPayment_DeveAlterarStatusDeApprovedParaPaid()
    {
        var request = CriarDraft();
        request.Submit(DateTimeOffset.UtcNow);
        request.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);

        request.RegisterPayment(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        Assert.Equal(RequestStatus.Paid, request.Status);
        Assert.NotNull(request.PaidAt);
        Assert.NotNull(request.PaidByUserId);
    }

    [Fact]
    public void Approve_DeveFalharQuandoSolicitacaoNaoEstaEnviada()
    {
        var request = CriarDraft();

        var action = () => request.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DomainRuleException>(action);
        Assert.Equal("A solicitação precisa estar enviada para ser aprovada.", exception.Message);
    }

    [Fact]
    public void RegisterPayment_DeveFalharQuandoSolicitacaoNaoEstaAprovada()
    {
        var request = CriarDraft();

        var action = () => request.RegisterPayment(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DomainRuleException>(action);
        Assert.Equal("Somente solicitações aprovadas podem ser pagas.", exception.Message);
    }

    private static ReimbursementRequest CriarDraft()
    {
        return new ReimbursementRequest(
            "RMB-20260404-ABC123",
            "Táxi aeroporto",
            Guid.NewGuid(),
            120m,
            "BRL",
            new DateOnly(2026, 4, 4),
            "Deslocamento para reunião externa",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
    }
}
