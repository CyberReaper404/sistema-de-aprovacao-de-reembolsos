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
            "Taxi aeroporto",
            Guid.NewGuid(),
            120,
            "BRL",
            new DateOnly(2026, 4, 4),
            "Deslocamento para reunião externa",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
    }
}
