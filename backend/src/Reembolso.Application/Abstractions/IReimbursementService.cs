using Reembolso.Application.Common;
using Reembolso.Application.Dtos.Reimbursements;

namespace Reembolso.Application.Abstractions;

public interface IReimbursementService
{
    Task<ReimbursementDetailResponse> CreateAsync(CreateReimbursementRequest request, CancellationToken cancellationToken);

    Task<ReimbursementDetailResponse> UpdateDraftAsync(Guid requestId, UpdateReimbursementDraftRequest request, CancellationToken cancellationToken);

    Task<ReimbursementDetailResponse> GetByIdAsync(Guid requestId, CancellationToken cancellationToken);

    Task<PagedResult<ReimbursementListItemResponse>> GetPagedAsync(ReimbursementListQuery query, CancellationToken cancellationToken);

    Task SubmitAsync(Guid requestId, CancellationToken cancellationToken);

    Task ApproveAsync(Guid requestId, ApproveReimbursementRequest request, CancellationToken cancellationToken);

    Task RejectAsync(Guid requestId, RejectReimbursementRequest request, CancellationToken cancellationToken);

    Task RecordPaymentAsync(Guid requestId, RecordPaymentRequest request, CancellationToken cancellationToken);

    Task<AttachmentResponse> AddAttachmentAsync(
        Guid requestId,
        string fileName,
        string contentType,
        long sizeInBytes,
        Stream content,
        CancellationToken cancellationToken);

    Task DeleteAttachmentAsync(Guid requestId, Guid attachmentId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AttachmentResponse>> GetAttachmentsAsync(Guid requestId, CancellationToken cancellationToken);

    Task<AttachmentDownloadResult> DownloadAttachmentAsync(Guid requestId, Guid attachmentId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WorkflowActionResponse>> GetWorkflowActionsAsync(Guid requestId, CancellationToken cancellationToken);
}

