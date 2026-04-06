using Reembolso.Infrastructure.Persistence;

namespace Reembolso.UnitTests;

public sealed class AppDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_DeveFalhar_QuandoConexaoNaoEstiverConfigurada()
    {
        var factory = new AppDbContextFactory();
        var previousValue = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);

            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext([]));
            Assert.Contains("não foi configurada", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previousValue);
        }
    }

    [Fact]
    public void CreateDbContext_DeveFalhar_QuandoConexaoEstiverEmFormatoInvalido()
    {
        var factory = new AppDbContextFactory();
        var previousValue = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host");

            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext([]));
            Assert.Contains("formato inválido", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previousValue);
        }
    }
}
