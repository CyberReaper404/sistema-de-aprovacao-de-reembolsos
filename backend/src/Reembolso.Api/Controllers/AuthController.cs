using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Reembolso.Application.Abstractions;
using Reembolso.Application.Dtos.Auth;
using Reembolso.Application.Exceptions;
using Reembolso.Infrastructure.Options;

namespace Reembolso.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshCookieName = "refresh_token";
    private readonly IAuthService _authService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly JwtOptions _jwtOptions;

    public AuthController(IAuthService authService, IHostEnvironment hostEnvironment, IOptions<JwtOptions> jwtOptions)
    {
        _authService = authService;
        _hostEnvironment = hostEnvironment;
        _jwtOptions = jwtOptions.Value;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await _authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(new
        {
            session.AccessToken,
            ExpiresAt = session.AccessTokenExpiresAt,
            session.User
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAppException("O cookie de refresh não foi encontrado.", "missing_refresh_cookie");
        }

        var session = await _authService.RefreshAsync(refreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        SetRefreshCookie(session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(new
        {
            session.AccessToken,
            ExpiresAt = session.AccessTokenExpiresAt,
            session.User
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken, cancellationToken);
        }

        DeleteRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await _authService.LogoutAllAsync(cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        return Ok(await _authService.GetCurrentUserAsync(cancellationToken));
    }

    private void SetRefreshCookie(string refreshToken, DateTimeOffset expiresAt)
    {
        var isDevelopment = _hostEnvironment.IsDevelopment();
        Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment || Request.IsHttps,
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
            Expires = expiresAt,
            IsEssential = true
        });
    }

    private void DeleteRefreshCookie()
    {
        var isDevelopment = _hostEnvironment.IsDevelopment();
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment || Request.IsHttps,
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(-_jwtOptions.RefreshTokenLifetimeDays)
        });
    }
}
