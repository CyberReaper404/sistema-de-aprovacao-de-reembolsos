using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Reembolso.Application.Dtos.Reimbursements;

namespace Reembolso.SecurityTests;

public sealed class AttachmentAccessSecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AttachmentAccessSecurityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Gestor_ForaDoEscopo_NaoDeveBaixarAnexoDeOutroCentroDeCusto()
    {
        using var ownerClient = _factory.CreateAuthenticatedClient("debora@empresa.test", "Senha@123");
        var (requestId, attachmentId) = await CriarSolicitacaoComAnexoAsync(ownerClient, "Despesa externa");

        using var managerClient = _factory.CreateAuthenticatedClient("bruno@empresa.test", "Senha@123");
        var response = await managerClient.GetAsync($"/api/reimbursements/{requestId}/attachments/{attachmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Colaborador_NaoDeveBaixarAnexoDeOutroColaborador()
    {
        using var ownerClient = _factory.CreateAuthenticatedClient("alice@empresa.test", "Senha@123");
        var (requestId, attachmentId) = await CriarSolicitacaoComAnexoAsync(ownerClient, "Despesa com anexo");

        using var otherCollaboratorClient = _factory.CreateAuthenticatedClient("carlos@empresa.test", "Senha@123");
        var response = await otherCollaboratorClient.GetAsync($"/api/reimbursements/{requestId}/attachments/{attachmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(Guid RequestId, Guid AttachmentId)> CriarSolicitacaoComAnexoAsync(HttpClient client, string title)
    {
        var createResponse = await client.PostAsJsonAsync("/api/reimbursements", new CreateReimbursementRequest(
            title,
            _factory.CategoryId,
            80m,
            "BRL",
            new DateOnly(2026, 4, 21),
            "Solicitação com comprovante"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ReimbursementDetailDto>();
        Assert.NotNull(created);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("%PDF-1.4 comprovante de teste"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "comprovante.pdf");

        var attachmentResponse = await client.PostAsync($"/api/reimbursements/{created!.Id}/attachments", form);
        attachmentResponse.EnsureSuccessStatusCode();
        var attachment = await attachmentResponse.Content.ReadFromJsonAsync<AttachmentDto>();
        Assert.NotNull(attachment);

        return (created.Id, attachment!.Id);
    }

    private sealed record ReimbursementDetailDto(Guid Id);

    private sealed record AttachmentDto(Guid Id);
}
