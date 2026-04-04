using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reembolso.Application.Abstractions;
using Reembolso.Infrastructure.Auditing;
using Reembolso.Infrastructure.Options;
using Reembolso.Infrastructure.Persistence;
using Reembolso.Infrastructure.Security;
using Reembolso.Infrastructure.Services;
using Reembolso.Infrastructure.Storage;

namespace Reembolso.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.Issuer) &&
                !string.IsNullOrWhiteSpace(options.Audience) &&
                !string.IsNullOrWhiteSpace(options.SigningKey) &&
                options.SigningKey.Length >= 32,
                "As configurações de JWT são inválidas.")
            .ValidateOnStart();

        services
            .AddOptions<AttachmentStorageOptions>()
            .Bind(configuration.GetSection(AttachmentStorageOptions.SectionName))
            .Validate(options => options.MaxFileSizeInBytes > 0, "O tamanho máximo de anexo deve ser maior que zero.")
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAttachmentStorage, LocalAttachmentStorage>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IReimbursementService, ReimbursementService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
