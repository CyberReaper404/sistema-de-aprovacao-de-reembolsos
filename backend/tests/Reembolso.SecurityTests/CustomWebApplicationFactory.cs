using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reembolso.Application.Dtos.Auth;
using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.SecurityTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static Guid CategoryId { get; private set; }

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=ignored",
                ["Jwt:Issuer"] = "reembolso-corporativo",
                ["Jwt:Audience"] = "reembolso-corporativo-web",
                ["Jwt:SigningKey"] = "trocar-esta-chave-em-producao-com-no-minimo-32-caracteres",
                ["AttachmentStorage:RootPath"] = Path.Combine(Path.GetTempPath(), "reembolso-security-tests", Guid.NewGuid().ToString("N")),
                ["AttachmentStorage:MaxFileSizeInBytes"] = "10485760"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await SeedAsync(dbContext);
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    public HttpClient CreateAuthenticatedClient(string email, string password)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
        var response = client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password)).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        var payload = response.Content.ReadFromJsonAsync<LoginResponse>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Resposta de login inválida.");

        if (string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException($"Login sem access token: {body}");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.AccessToken);
        var meResponse = client.GetAsync("/api/auth/me").GetAwaiter().GetResult();
        if (!meResponse.IsSuccessStatusCode)
        {
            var body = meResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var authHeader = string.Join(" | ", meResponse.Headers.WwwAuthenticate.Select(x => x.ToString()));
            throw new InvalidOperationException($"Falha ao validar sessão de teste: {(int)meResponse.StatusCode} - auth={authHeader} - token={payload.AccessToken} - body={body}");
        }

        return client;
    }

    private static async Task SeedAsync(AppDbContext dbContext)
    {
        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var hasher = new PasswordHasher<object>();
        var marker = new object();
        var now = DateTimeOffset.UtcNow;

        var costCenter = new CostCenter("FIN-001", "Financeiro", now);
        var category = new ReimbursementCategory("Transporte", "Despesas com deslocamento", 500, 100, now);
        CategoryId = category.Id;

        dbContext.CostCenters.Add(costCenter);
        dbContext.ReimbursementCategories.Add(category);
        dbContext.Users.AddRange(
            new User("Alice Colaboradora", "alice@empresa.test", hasher.HashPassword(marker, "Senha@123"), UserRole.Collaborator, costCenter.Id, now),
            new User("Carlos Colaborador", "carlos@empresa.test", hasher.HashPassword(marker, "Senha@123"), UserRole.Collaborator, costCenter.Id, now));

        await dbContext.SaveChangesAsync();
    }

    private sealed record LoginResponse([property: JsonPropertyName("accessToken")] string AccessToken);
}
