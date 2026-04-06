using System.Net;
using System.Net.Http.Json;

namespace Reembolso.SecurityTests;

public sealed class UnauthenticatedAccessTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UnauthenticatedAccessTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EndpointsProtegidos_DevemNegarAcessoSemToken()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var detailResponse = await client.GetAsync($"/api/reimbursements/{Guid.NewGuid()}");
        var listResponse = await client.GetAsync("/api/reimbursements?page=1&pageSize=10");
        var approveResponse = await client.PostAsJsonAsync($"/api/reimbursements/{Guid.NewGuid()}/approve", new { comment = "Sem autenticação" });
        var paymentResponse = await client.PostAsJsonAsync($"/api/reimbursements/{Guid.NewGuid()}/payment", new
        {
            paymentMethod = 1,
            paymentReference = "SEM-AUTENTICACAO",
            paidAt = DateTimeOffset.UtcNow,
            amountPaid = 10m,
            notes = "Tentativa sem token"
        });
        var adminResponse = await client.GetAsync("/api/admin/users?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, detailResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, approveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, paymentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, adminResponse.StatusCode);
    }
}
