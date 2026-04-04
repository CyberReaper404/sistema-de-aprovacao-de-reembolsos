namespace Reembolso.Infrastructure.Options;

public sealed class AttachmentStorageOptions
{
    public const string SectionName = "AttachmentStorage";

    public string RootPath { get; init; } = "storage/attachments";

    public long MaxFileSizeInBytes { get; init; } = 10 * 1024 * 1024;
}

