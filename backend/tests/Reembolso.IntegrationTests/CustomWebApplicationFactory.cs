using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Reembolso.Application.Abstractions;
using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public Guid CostCenterId { get; private set; }
    public Guid CategoryId { get; private set; }

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Dictionary<string, string?> _environmentVariables = new();
    private string _attachmentRootPath = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        _attachmentRootPath = Path.Combine(Path.GetTempPath(), "reembolso-tests", Guid.NewGuid().ToString("N"));
        SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=example.invalid;Database=ignored;Username=ignored;Password=ignored");
        SetEnvironmentVariable("Jwt__Issuer", "reembolso-corporativo");
        SetEnvironmentVariable("Jwt__Audience", "reembolso-corporativo-web");
        SetEnvironmentVariable("Jwt__SigningKey", "chave-de-teste-com-mais-de-trinta-e-dois-caracteres");
        SetEnvironmentVariable("AttachmentStorage__RootPath", _attachmentRootPath);
        SetEnvironmentVariable("AttachmentStorage__MaxFileSizeInBytes", "10485760");

        await _connection.OpenAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await SeedAsync(dbContext);
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();

        foreach (var variableName in _environmentVariables.Keys)
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }

        if (!string.IsNullOrWhiteSpace(_attachmentRootPath) && Directory.Exists(_attachmentRootPath))
        {
            Directory.Delete(_attachmentRootPath, true);
        }
    }

    public HttpClient CreateAuthenticatedClient(string email, string password)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var user = dbContext.Users.SingleOrDefault(x => x.Email == email)
            ?? throw new InvalidOperationException($"Usuário de teste não encontrado: {email}");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.CreateAccessToken(user));

        return client;
    }

    private async Task SeedAsync(AppDbContext dbContext)
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

        CostCenterId = costCenter.Id;
        CategoryId = category.Id;

        var collaborator = new User("Alice Colaboradora", "alice@empresa.test", hasher.HashPassword(marker, "Senha@123"), UserRole.Collaborator, costCenter.Id, now);
        var secondCollaborator = new User("Carlos Colaborador", "carlos@empresa.test", hasher.HashPassword(marker, "Senha@123"), UserRole.Collaborator, costCenter.Id, now);
        var manager = new User("Bruno Gestor", "bruno@empresa.test", hasher.HashPassword(marker, "Senha@123"), UserRole.Manager, costCenter.Id, now);
        var finance = new User("Fernanda Financeiro", "fernanda@empresa.test", hasher.HashPassword(marker, "Senha@123"), UserRole.Finance, costCenter.Id, now);
        var admin = new User("Ana Admin", "admin@empresa.test", hasher.HashPassword(marker, "Senha@123"), UserRole.Administrator, costCenter.Id, now);

        dbContext.CostCenters.Add(costCenter);
        dbContext.ReimbursementCategories.Add(category);
        dbContext.Users.AddRange(collaborator, secondCollaborator, manager, finance, admin);
        dbContext.ManagerCostCenterScopes.Add(new ManagerCostCenterScope(manager.Id, costCenter.Id, now));
        await dbContext.SaveChangesAsync();
    }

    private void SetEnvironmentVariable(string variableName, string value)
    {
        _environmentVariables[variableName] = value;
        Environment.SetEnvironmentVariable(variableName, value);
    }
}
