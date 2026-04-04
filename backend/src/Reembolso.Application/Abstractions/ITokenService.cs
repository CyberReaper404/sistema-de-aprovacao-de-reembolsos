using Reembolso.Domain.Entities;

namespace Reembolso.Application.Abstractions;

public interface ITokenService
{
    string CreateAccessToken(User user);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

