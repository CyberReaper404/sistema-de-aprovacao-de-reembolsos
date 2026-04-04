namespace Reembolso.Application.Abstractions;

public interface IPasswordHasherService
{
    string HashPassword(string password);

    bool VerifyHashedPassword(string passwordHash, string password);
}

