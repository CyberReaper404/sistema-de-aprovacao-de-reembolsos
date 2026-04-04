using System.Net;
using System.Net.Http.Json;
using Reembolso.Application.Dtos.Reimbursements;
using Reembolso.Domain.Enums;

namespace Reembolso.SecurityTests;

public sealed class RequestIsolationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestIsolationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Colaborador_NaoDeveConsultarSolicitacaoDeOutroColaborador()
    {
        using var aliceClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var createResponse = await aliceClient.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            "Taxi",
            _factory.CategoryId,
            80,
            "BRL",
            new DateOnly(2026, 4, 6),
            "Deslocamento"));

        var created = await createResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);
        Assert.Equal(RequestStatus.Draft, created!.Status);

        using var carlosClient = _factory.CreateAuthenticatedClient("carlos@empresa.test", "Senha@123");
        var detailResponse = await carlosClient.GetAsync($"/api/reimbursements/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, detailResponse.StatusCode);
    }

    private sealed record ReimbursementDetailDto(Guid Id, RequestStatus Status);
}
