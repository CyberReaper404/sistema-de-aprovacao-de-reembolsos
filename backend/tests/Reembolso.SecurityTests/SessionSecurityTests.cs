using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reembolso.Domain.Entities;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.SecurityTests;

public sealed class SessionSecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SessionSecurityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Refresh_DeveFalharQuandoSessaoEstiverRevogada()
    {
        var loginSession = await RealizarLoginAsync("alice@empresa.test", "Senha@123");
        await RevogarSessaoAsync(loginSession.Email);

        using var client = CriarClienteComCookie(loginSession.RefreshCookieHeader);
        var response = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"revoked_refresh_token\"", body);
    }

    [Fact]
    public async Task Refresh_DeveFalharQuandoTokenEstiverExpirado()
    {
        var loginSession = await RealizarLoginAsync("alice@empresa.test", "Senha@123");
        await ExpirarSessaoAsync(loginSession.Email);

        using var client = CriarClienteComCookie(loginSession.RefreshCookieHeader);
        var response = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"expired_refresh_token\"", body);
    }

    [Fact]
    public async Task TokenComSessionVersionDesatualizada_DeveSerRejeitado()
    {
        using var client = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await dbContext.Users.SingleAsync(x => x.Email == "alice@empresa.test");
            user.RevokeAllSessions(DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"unauthorized\"", body);
    }

    [Fact]
    public async Task TokenMalformado_DeveSerRejeitado()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-malformado");

        var response = await client.GetAsync("/api/reimbursements?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"unauthorized\"", body);
    }

    private async Task<LoginSession> RealizarLoginAsync(string email, string password)
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);

        var setCookie = response.Headers.GetValues("Set-Cookie").Single();
        var refreshToken = ExtrairRefreshToken(setCookie);

        return new LoginSession(email, $"refresh_token={refreshToken}");
    }

    private HttpClient CriarClienteComCookie(string cookieHeader)
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        return client;
    }

    private async Task RevogarSessaoAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = (await dbContext.RefreshSessions
            .Include(x => x.User)
            .Where(x => x.User != null && x.User.Email == email)
            .ToListAsync())
            .OrderByDescending(x => x.CreatedAt)
            .First();
        session.Revoke(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync();
    }

    private async Task ExpirarSessaoAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = (await dbContext.RefreshSessions
            .Include(x => x.User)
            .Where(x => x.User != null && x.User.Email == email)
            .ToListAsync())
            .OrderByDescending(x => x.CreatedAt)
            .First();
        dbContext.Entry(session).Property(nameof(RefreshSession.ExpiresAt)).CurrentValue = DateTimeOffset.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();
    }

    private static string ExtrairRefreshToken(string setCookieHeader)
    {
        var segment = setCookieHeader.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Single(x => x.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase));

        var rawValue = segment["refresh_token=".Length..].Trim('"');
        return Uri.UnescapeDataString(rawValue);
    }

    private sealed record LoginResponse(string AccessToken);

    private sealed record LoginSession(string Email, string RefreshCookieHeader);
}
