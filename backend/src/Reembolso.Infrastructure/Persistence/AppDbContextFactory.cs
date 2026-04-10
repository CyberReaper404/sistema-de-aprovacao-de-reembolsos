using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Reembolso.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var skipLocalConfig = string.Equals(
            Environment.GetEnvironmentVariable("REEMBOLSO_SKIP_LOCAL_DESIGN_CONFIG"),
            "1",
            StringComparison.Ordinal);
        var basePath = ResolveApiBasePath();
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true);

        if (!skipLocalConfig)
        {
            builder.AddJsonFile("appsettings.Local.json", optional: true);
        }

        var configuration = builder
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("A conexão com o banco não foi configurada para operações de design time.");
        }

        try
        {
            _ = new NpgsqlConnectionStringBuilder(connectionString);

            if (!connectionString.Contains('='))
            {
                throw new FormatException("A conexão com o banco está em formato inválido.");
            }
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            throw new InvalidOperationException("A conexão com o banco está em formato inválido.", exception);
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveApiBasePath()
    {
        foreach (var root in EnumerateSearchRoots())
        {
            foreach (var candidate in EnumerateCandidates(root))
            {
                if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                {
                    return candidate;
                }
            }
        }

        throw new InvalidOperationException("Não foi possível localizar a pasta da API para carregar a configuração de design time.");
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
    }

    private static IEnumerable<string> EnumerateCandidates(string startPath)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = new DirectoryInfo(Path.GetFullPath(startPath));

        while (current is not null)
        {
            var directApi = Path.Combine(current.FullName, "src", "Reembolso.Api");
            if (visited.Add(directApi))
            {
                yield return directApi;
            }

            var siblingApi = Path.Combine(current.FullName, "..", "Reembolso.Api");
            siblingApi = Path.GetFullPath(siblingApi);
            if (visited.Add(siblingApi))
            {
                yield return siblingApi;
            }

            if (visited.Add(current.FullName))
            {
                yield return current.FullName;
            }

            current = current.Parent;
        }
    }
}
