using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Reembolso.Application.Dtos.Admin;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.IntegrationTests;

public sealed class AdministrativeConsistencyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdministrativeConsistencyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_NaoDeveAssociarCentroInativoAoGestor()
    {
        using var adminClient = _factory.CreateAuthenticatedClient("admin@empresa.test", "Senha@123");

        var createCostCenterResponse = await adminClient.PostAsJsonAsync("/api/admin/cost-centers", new CreateCostCenterRequest("TEC-002", "Tecnologia"));
        createCostCenterResponse.EnsureSuccessStatusCode();
        var costCenter = await createCostCenterResponse.Content.ReadFromJsonAsync<CostCenterResponse>();
        Assert.NotNull(costCenter);

        var updateCostCenterResponse = await adminClient.PutAsJsonAsync(
            $"/api/admin/cost-centers/{costCenter!.Id}",
            new UpdateCostCenterRequest(costCenter.Code, costCenter.Name, false));
        updateCostCenterResponse.EnsureSuccessStatusCode();

        var createUserResponse = await adminClient.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            "Marina Gestora",
            "marina@empresa.test",
            "Senha@123",
            Reembolso.Domain.Enums.UserRole.Manager,
            _factory.CostCenterId,
            new[] { costCenter.Id }));

        Assert.Equal(HttpStatusCode.BadRequest, createUserResponse.StatusCode);
        var payload = await createUserResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(payload);
    }

    [Fact]
    public async Task Admin_DeveGerarAuditoriaAoCriarEAtualizarCategoriaECentroDeCusto()
    {
        using var adminClient = _factory.CreateAuthenticatedClient("admin@empresa.test", "Senha@123");

        var createCostCenterResponse = await adminClient.PostAsJsonAsync("/api/admin/cost-centers", new CreateCostCenterRequest("OPS-003", "Operacoes"));
        createCostCenterResponse.EnsureSuccessStatusCode();
        var createdCostCenter = await createCostCenterResponse.Content.ReadFromJsonAsync<CostCenterResponse>();

        var createCategoryResponse = await adminClient.PostAsJsonAsync("/api/admin/categories", new CreateCategoryRequest("Hospedagem", "Diarias", 1500, 200));
        createCategoryResponse.EnsureSuccessStatusCode();
        var createdCategory = await createCategoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var updateCostCenterResponse = await adminClient.PutAsJsonAsync(
            $"/api/admin/cost-centers/{createdCostCenter!.Id}",
            new UpdateCostCenterRequest(createdCostCenter.Code, "Operacoes Corporativas", true));
        updateCostCenterResponse.EnsureSuccessStatusCode();

        var updateCategoryResponse = await adminClient.PutAsJsonAsync(
            $"/api/admin/categories/{createdCategory!.Id}",
            new UpdateCategoryRequest(createdCategory.Name, "Diarias nacionais", 1800, 250, true));
        updateCategoryResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var events = dbContext.AuditEntries
            .Select(x => x.EventType)
            .ToList();

        Assert.Contains("admin.cost_center_created", events);
        Assert.Contains("admin.cost_center_updated", events);
        Assert.Contains("admin.category_created", events);
        Assert.Contains("admin.category_updated", events);
    }
}
