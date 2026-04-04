using System.Net;
using System.Net.Http.Json;
using Reembolso.Application.Dtos.Reimbursements;
using Reembolso.Domain.Enums;

namespace Reembolso.IntegrationTests;

public sealed class AuthAndReimbursementFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthAndReimbursementFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_DeveRetornarAccessTokenERefreshCookie()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "alice@empresa.test", password = "Senha@123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers, header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Colaborador_DeveCriarERmeterSolicitacao()
    {
        using var client = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");

        var createResponse = await client.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            "Taxi aeroporto",
            CustomWebApplicationFactory.CategoryId,
            80,
            "BRL",
            new DateOnly(2026, 4, 4),
            "Deslocamento para reuniao"));

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);
        Assert.Equal(RequestStatus.Draft, created!.Status);

        var submitResponse = await client.PostAsync($"/api/reimbursements/{created.Id}/submit", null);
        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        var detailResponse = await client.GetAsync($"/api/reimbursements/{created.Id}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.Equal(RequestStatus.Submitted, detail!.Status);
    }

    [Fact]
    public async Task GestorDoCentroDeCusto_DeveAprovarSolicitacaoEnviada()
    {
        using var collaborator = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");

        var createResponse = await collaborator.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            "Hotel",
            CustomWebApplicationFactory.CategoryId,
            90,
            "BRL",
            new DateOnly(2026, 4, 5),
            "Hospedagem de viagem"));

        var created = await createResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        await collaborator.PostAsync($"/api/reimbursements/{created!.Id}/submit", null);

        using var manager = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var approveResponse = await manager.PostAsJsonAsync($"/api/reimbursements/{created.Id}/approve", new ApproveReimbursementRequest("Aprovado"));
        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        var detail = await manager.GetFromJsonAsync<ReimbursementDetailDto>($"/api/reimbursements/{created.Id}");
        Assert.Equal(RequestStatus.Approved, detail!.Status);
    }

    private sealed record ReimbursementDetailDto(Guid Id, RequestStatus Status);
}
