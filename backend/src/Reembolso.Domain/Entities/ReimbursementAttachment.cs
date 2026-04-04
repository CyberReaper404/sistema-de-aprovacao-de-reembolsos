namespace Reembolso.Domain.Entities;

public class ReimbursementAttachment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RequestId { get; private set; }
    public ReimbursementRequest? Request { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoredFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeInBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private ReimbursementAttachment()
    {
    }

    public ReimbursementAttachment(
        Guid requestId,
        string originalFileName,
        string storedFileName,
        string contentType,
        long sizeInBytes,
        string sha256,
        Guid uploadedByUserId,
        DateTimeOffset now)
    {
        RequestId = requestId;
        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        Sha256 = sha256;
        UploadedByUserId = uploadedByUserId;
        CreatedAt = now;
    }
}

