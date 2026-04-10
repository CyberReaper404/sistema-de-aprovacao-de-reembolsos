using Reembolso.Application.Abstractions;
using Reembolso.Infrastructure.Options;

namespace Reembolso.Infrastructure.Storage;

public sealed class LocalAttachmentStorage : IAttachmentStorage
{
    private readonly string _rootPath;

    public LocalAttachmentStorage(Microsoft.Extensions.Options.IOptions<AttachmentStorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(_rootPath, storedFileName);

        Directory.CreateDirectory(_rootPath);
        await using var fileStream = File.Create(destinationPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return storedFileName;
    }

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var path = Path.Combine(_rootPath, storedFileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Arquivo de anexo não encontrado no armazenamento.", path);
        }

        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var path = Path.Combine(_rootPath, storedFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
