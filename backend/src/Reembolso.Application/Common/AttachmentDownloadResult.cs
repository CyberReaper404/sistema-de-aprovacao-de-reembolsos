namespace Reembolso.Application.Common;

public sealed record AttachmentDownloadResult(
    Stream Content,
    string FileName,
    string ContentType);

