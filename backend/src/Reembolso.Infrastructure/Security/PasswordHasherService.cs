using Microsoft.AspNetCore.Identity;
using Reembolso.Application.Abstractions;

namespace Reembolso.Infrastructure.Security;

public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<object> _passwordHasher = new();
    private static readonly object Marker = new();

    public string HashPassword(string password) => _passwordHasher.HashPassword(Marker, password);

    public bool VerifyHashedPassword(string passwordHash, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(Marker, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}

