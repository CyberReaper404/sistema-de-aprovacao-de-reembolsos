namespace Reembolso.Application.Abstractions;

public interface IAttachmentStorage
{
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);

    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}

