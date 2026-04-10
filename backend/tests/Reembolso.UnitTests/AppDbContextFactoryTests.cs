using Reembolso.Infrastructure.Persistence;

namespace Reembolso.UnitTests;

public sealed class AppDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_DeveFalhar_QuandoConexaoNaoEstiverConfigurada()
    {
        var factory = new AppDbContextFactory();
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        var previousFlag = Environment.GetEnvironmentVariable("REEMBOLSO_SKIP_LOCAL_DESIGN_CONFIG");

        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
            Environment.SetEnvironmentVariable("REEMBOLSO_SKIP_LOCAL_DESIGN_CONFIG", "1");

            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext([]));
            Assert.Contains("não foi configurada", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previousConnection);
            Environment.SetEnvironmentVariable("REEMBOLSO_SKIP_LOCAL_DESIGN_CONFIG", previousFlag);
        }
    }

    [Fact]
    public void CreateDbContext_DeveFalhar_QuandoConexaoEstiverEmFormatoInvalido()
    {
        var factory = new AppDbContextFactory();
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        var previousFlag = Environment.GetEnvironmentVariable("REEMBOLSO_SKIP_LOCAL_DESIGN_CONFIG");

        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host");
            Environment.SetEnvironmentVariable("REEMBOLSO_SKIP_LOCAL_DESIGN_CONFIG", "1");

            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext([]));
            Assert.Contains("formato inválido", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previousConnection);
            Environment.SetEnvironmentVariable("REEMBOLSO_SKIP_LOCAL_DESIGN_CONFIG", previousFlag);
        }
    }
}
