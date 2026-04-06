using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reembolso.Application.Dtos.Reimbursements;
using Reembolso.Infrastructure.Options;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.IntegrationTests;

public sealed class AuthenticationAndAttachmentConsistencyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationAndAttachmentConsistencyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Refresh_SemCookie_DeveUsarProblemDetailsPadrao()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"missing_refresh_cookie\"", body);
        Assert.Contains("\"traceId\":", body);
    }

    [Fact]
    public async Task Login_DeveDefinirCookieComSameSiteDeDesenvolvimento()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "alice@empresa.test", password = "Senha@123" });

        response.EnsureSuccessStatusCode();
        var cookieHeader = response.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant();
        Assert.Contains("refresh_token=", cookieHeader);
        Assert.DoesNotContain("samesite=strict", cookieHeader);
    }

    [Fact]
    public async Task Refresh_DeveRetornarContratoExplicitoSemRefreshTokenNoCorpo()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email = "alice@empresa.test", password = "Senha@123" });
        loginResponse.EnsureSuccessStatusCode();

        var refreshResponse = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.Contains(refreshResponse.Headers, header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));

        using var body = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());
        var root = body.RootElement;

        Assert.True(root.TryGetProperty("accessToken", out var accessToken));
        Assert.False(string.IsNullOrWhiteSpace(accessToken.GetString()));
        Assert.True(root.TryGetProperty("accessTokenExpiresAt", out _));
        Assert.True(root.TryGetProperty("refreshTokenExpiresAt", out _));
        Assert.True(root.TryGetProperty("user", out var user));
        Assert.Equal("alice@empresa.test", user.GetProperty("email").GetString());
        Assert.False(root.TryGetProperty("refreshToken", out _));
    }

    [Fact]
    public async Task Download_DeveRetornarNaoEncontrado_QuandoArquivoFisicoNaoExistir()
    {
        using var client = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");

        var createResponse = await client.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            "Taxi",
            _factory.CategoryId,
            90,
            "BRL",
            new DateOnly(2026, 4, 7),
            "Deslocamento urbano"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("%PDF-1.4 teste"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "comprovante.pdf");

        var attachmentResponse = await client.PostAsync($"/api/reimbursements/{created!.Id}/attachments", form);
        attachmentResponse.EnsureSuccessStatusCode();
        var attachment = await attachmentResponse.Content.ReadFromJsonAsync<AttachmentDto>();
        Assert.NotNull(attachment);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storageOptions = scope.ServiceProvider.GetRequiredService<IOptions<AttachmentStorageOptions>>();
            var storedFileName = dbContext.ReimbursementAttachments
                .Where(x => x.Id == attachment!.Id)
                .Select(x => x.StoredFileName)
                .Single();

            var fullPath = Path.Combine(Path.GetFullPath(storageOptions.Value.RootPath), storedFileName);
            File.Delete(fullPath);
        }

        var downloadResponse = await client.GetAsync($"/api/reimbursements/{created.Id}/attachments/{attachment!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, downloadResponse.StatusCode);
        var body = await downloadResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"errorCode\":\"attachment_file_missing\"", body);
    }

    private sealed record ReimbursementDetailDto(Guid Id);

    private sealed record AttachmentDto(Guid Id);
}
