using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Reembolso.Application.Abstractions;
using Reembolso.Application.Common;
using Reembolso.Application.Dtos.Reimbursements;
using Reembolso.Application.Exceptions;
using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;
using Reembolso.Domain.Exceptions;
using Reembolso.Infrastructure.Options;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.Infrastructure.Services;

public sealed class ReimbursementService : IReimbursementService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    private static readonly DecisionReasonCode[] ComplementationReasonCodes =
    [
        DecisionReasonCode.NeedMoreDetails,
        DecisionReasonCode.NeedAdditionalDocument,
        DecisionReasonCode.Other
    ];

    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAttachmentStorage _attachmentStorage;
    private readonly IAuditService _auditService;
    private readonly AttachmentStorageOptions _storageOptions;

    public ReimbursementService(
        AppDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IAttachmentStorage attachmentStorage,
        IAuditService auditService,
        IOptions<AttachmentStorageOptions> storageOptions)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _attachmentStorage = attachmentStorage;
        _auditService = auditService;
        _storageOptions = storageOptions.Value;
    }

    public async Task<ReimbursementDetailResponse> CreateAsync(CreateReimbursementRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        EnsureRole(UserRole.Collaborator);
        ValidateDraftPayload(request.Title, request.Amount, request.Currency, request.Description, request.ExpenseDate);

        var category = await GetActiveCategoryAsync(request.CategoryId, cancellationToken);
        ValidateCategoryPolicy(category, request.Amount);

        var now = _dateTimeProvider.UtcNow;
        var entity = new ReimbursementRequest(
            GenerateRequestNumber(now),
            request.Title,
            request.CategoryId,
            request.Amount,
            request.Currency,
            request.ExpenseDate,
            request.Description,
            user.PrimaryCostCenterId,
            user.Id,
            now);

        _dbContext.ReimbursementRequests.Add(entity);
        _dbContext.WorkflowActions.Add(new WorkflowAction(entity.Id, WorkflowActionType.DraftCreated, null, RequestStatus.Draft, user.Id, null, null, now));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReimbursementCategoryOptionResponse>> GetAvailableCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ReimbursementCategories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ReimbursementCategoryOptionResponse(
                x.Id,
                x.Name,
                x.Description,
                x.MaxAmount,
                x.ReceiptRequiredAboveAmount,
                x.ReceiptRequiredAlways,
                x.SubmissionDeadlineDays))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReimbursementDetailResponse> UpdateDraftAsync(Guid requestId, UpdateReimbursementDraftRequest request, CancellationToken cancellationToken)
    {
        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        EnsureOwner(entity);
        EnsureRowVersion(entity, request.RowVersion);

        ValidateDraftPayload(request.Title, request.Amount, request.Currency, request.Description, request.ExpenseDate);
        ValidateCategoryPolicy(await GetActiveCategoryAsync(request.CategoryId, cancellationToken), request.Amount);

        var now = _dateTimeProvider.UtcNow;
        ExecuteDomainTransition(() => entity.UpdateDraft(request.Title, request.CategoryId, request.Amount, request.Currency, request.ExpenseDate, request.Description, now));
        _dbContext.WorkflowActions.Add(new WorkflowAction(
            entity.Id,
            WorkflowActionType.DraftUpdated,
            entity.Status,
            entity.Status,
            RequireCurrentUserId(),
            null,
            entity.HasPendingComplementation ? "Complementação atualizada." : null,
            now));

        await SaveChangesHandlingConcurrencyAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<ReimbursementDetailResponse> GetByIdAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        await EnsureCanReadAsync(entity, cancellationToken);
        return await BuildDetailAsync(entity, cancellationToken);
    }

    public async Task<PagedResult<ReimbursementListItemResponse>> GetPagedAsync(ReimbursementListQuery query, CancellationToken cancellationToken)
    {
        await EnsureRequestedCostCenterIsAllowedAsync(query.CostCenterId, cancellationToken);
        var scopedQuery = BuildScopedQueryAsync();
        IQueryable<ReimbursementRequest> dbQuery = scopedQuery
            .Include(x => x.Category)
            .Include(x => x.CostCenter);

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Status == query.Status.Value);
        }

        if (query.CategoryId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CategoryId == query.CategoryId.Value);
        }

        if (query.CostCenterId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CostCenterId == query.CostCenterId.Value);
        }

        if (query.ExpenseDateFrom.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.ExpenseDate >= query.ExpenseDateFrom.Value);
        }

        if (query.ExpenseDateTo.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.ExpenseDate <= query.ExpenseDateTo.Value);
        }

        if (query.CreatedFrom.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAt <= query.CreatedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.RequestNumber))
        {
            dbQuery = dbQuery.Where(x => x.RequestNumber.Contains(query.RequestNumber.Trim()));
        }

        if (query.CreatedByMe && _currentUserContext.UserId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedByUserId == _currentUserContext.UserId.Value);
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalItems = await dbQuery.CountAsync(cancellationToken);

        var projectedItems = await dbQuery
            .Select(x => new ReimbursementListItemResponse(
                x.Id,
                x.RequestNumber,
                x.Title,
                x.Category!.Name,
                x.Amount,
                x.Currency,
                x.Status,
                x.ExpenseDate,
                x.CostCenter!.Code,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var items = ApplyOrdering(projectedItems, query.Sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PagedResult<ReimbursementListItemResponse>(items, page, pageSize, totalItems, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task SubmitAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        EnsureOwner(entity);
        EnsureAttachmentPolicy(entity);
        EnsureSubmissionDeadline(entity);

        var now = _dateTimeProvider.UtcNow;
        var userId = RequireCurrentUserId();

        if (entity.Status == RequestStatus.Draft)
        {
            ExecuteDomainTransition(() => entity.Submit(now));
            _dbContext.WorkflowActions.Add(new WorkflowAction(entity.Id, WorkflowActionType.Submitted, RequestStatus.Draft, RequestStatus.Submitted, userId, null, null, now));
        }
        else
        {
            ExecuteDomainTransition(() => entity.ResubmitAfterComplementation(now));
            _dbContext.WorkflowActions.Add(new WorkflowAction(entity.Id, WorkflowActionType.Submitted, RequestStatus.Submitted, RequestStatus.Submitted, userId, null, "Complementação enviada.", now));
        }

        await SaveChangesHandlingConcurrencyAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid requestId, ApproveReimbursementRequest request, CancellationToken cancellationToken)
    {
        ValidateApprovalDecision(request.ReasonCode, request.Comment);

        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        await EnsureManagerScopeAsync(entity, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var userId = RequireCurrentUserId();
        var decisionComment = request.ReasonCode.HasValue
            ? BuildDecisionComment(request.ReasonCode.Value, request.Comment)
            : NormalizeOptionalComment(request.Comment);

        ExecuteDomainTransition(() => entity.Approve(userId, request.ReasonCode, decisionComment, now));
        _dbContext.WorkflowActions.Add(new WorkflowAction(
            entity.Id,
            WorkflowActionType.Approved,
            RequestStatus.Submitted,
            RequestStatus.Approved,
            userId,
            request.ReasonCode,
            decisionComment,
            now));

        await SaveChangesHandlingConcurrencyAsync(cancellationToken);
        await _auditService.WriteAsync("reimbursement.approved", "reimbursement_request", entity.Id.ToString(), AuditSeverity.Information, null, cancellationToken);
    }

    public async Task RejectAsync(Guid requestId, RejectReimbursementRequest request, CancellationToken cancellationToken)
    {
        ValidateRequiredDecisionReason(request.ReasonCode, request.Comment, "reject");

        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        await EnsureManagerScopeAsync(entity, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var userId = RequireCurrentUserId();
        var decisionComment = BuildDecisionComment(request.ReasonCode, request.Comment);

        ExecuteDomainTransition(() => entity.Reject(userId, request.ReasonCode, decisionComment, now));
        _dbContext.WorkflowActions.Add(new WorkflowAction(
            entity.Id,
            WorkflowActionType.Rejected,
            RequestStatus.Submitted,
            RequestStatus.Rejected,
            userId,
            request.ReasonCode,
            decisionComment,
            now));

        await SaveChangesHandlingConcurrencyAsync(cancellationToken);
        await _auditService.WriteAsync("reimbursement.rejected", "reimbursement_request", entity.Id.ToString(), AuditSeverity.Warning, null, cancellationToken);
    }

    public async Task RequestComplementationAsync(Guid requestId, RequestComplementationRequest request, CancellationToken cancellationToken)
    {
        ValidateRequiredDecisionReason(request.ReasonCode, request.Comment, "complementation", ComplementationReasonCodes);

        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        await EnsureManagerScopeAsync(entity, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var userId = RequireCurrentUserId();
        var decisionComment = BuildDecisionComment(request.ReasonCode, request.Comment);

        ExecuteDomainTransition(() => entity.RequestComplementation(userId, request.ReasonCode, decisionComment, now));
        _dbContext.WorkflowActions.Add(new WorkflowAction(
            entity.Id,
            WorkflowActionType.ComplementationRequested,
            RequestStatus.Submitted,
            RequestStatus.Submitted,
            userId,
            request.ReasonCode,
            decisionComment,
            now));

        await SaveChangesHandlingConcurrencyAsync(cancellationToken);
        await _auditService.WriteAsync("reimbursement.complementation_requested", "reimbursement_request", entity.Id.ToString(), AuditSeverity.Information, null, cancellationToken);
    }

    public async Task RecordPaymentAsync(Guid requestId, RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.Finance);
        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);

        if (entity.PaymentRecord is not null)
        {
            throw new ConflictAppException("A solicitação já possui pagamento registrado.");
        }

        if (request.AmountPaid != entity.Amount)
        {
            throw Validation("O valor pago deve ser igual ao valor aprovado.", new Dictionary<string, string[]>
            {
                ["amountPaid"] = ["O valor pago deve ser igual ao valor aprovado."]
            });
        }

        if (request.PaidAt < entity.ApprovedAt)
        {
            throw Validation("A data de pagamento não pode ser anterior à aprovação.", new Dictionary<string, string[]>
            {
                ["paidAt"] = ["A data de pagamento não pode ser anterior à aprovação."]
            });
        }

        var now = _dateTimeProvider.UtcNow;
        var userId = RequireCurrentUserId();
        ExecuteDomainTransition(() => entity.RegisterPayment(userId, request.PaidAt, now));

        _dbContext.PaymentRecords.Add(new PaymentRecord(entity.Id, userId, request.PaidAt, request.PaymentMethod, request.PaymentReference, request.AmountPaid, request.Notes, now));
        _dbContext.WorkflowActions.Add(new WorkflowAction(entity.Id, WorkflowActionType.PaymentRegistered, RequestStatus.Approved, RequestStatus.Paid, userId, null, request.PaymentReference, now));
        await SaveChangesHandlingConcurrencyAsync(cancellationToken);
        await _auditService.WriteAsync("reimbursement.paid", "reimbursement_request", entity.Id.ToString(), AuditSeverity.Information, null, cancellationToken);
    }

    public async Task<AttachmentResponse> AddAttachmentAsync(Guid requestId, string fileName, string contentType, long sizeInBytes, Stream content, CancellationToken cancellationToken)
    {
        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        EnsureOwner(entity);

        if (entity.Status != RequestStatus.Draft && !(entity.Status == RequestStatus.Submitted && entity.HasPendingComplementation))
        {
            throw new ConflictAppException("Somente rascunhos ou solicitações com complementação pendente aceitam anexos.");
        }

        ValidateAttachment(fileName, contentType, sizeInBytes);

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var fileBytes = buffer.ToArray();
        var sha256 = Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant();

        await using var uploadStream = new MemoryStream(fileBytes);
        var storedFileName = await _attachmentStorage.SaveAsync(fileName, uploadStream, cancellationToken);

        try
        {
            var attachment = new ReimbursementAttachment(
                entity.Id,
                fileName,
                storedFileName,
                contentType,
                sizeInBytes,
                sha256,
                RequireCurrentUserId(),
                _dateTimeProvider.UtcNow);

            _dbContext.ReimbursementAttachments.Add(attachment);
            _dbContext.WorkflowActions.Add(new WorkflowAction(entity.Id, WorkflowActionType.AttachmentAdded, entity.Status, entity.Status, RequireCurrentUserId(), null, fileName, _dateTimeProvider.UtcNow));
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new AttachmentResponse(attachment.Id, attachment.OriginalFileName, attachment.ContentType, attachment.SizeInBytes, attachment.CreatedAt);
        }
        catch
        {
            await _attachmentStorage.DeleteAsync(storedFileName, cancellationToken);
            throw;
        }
    }

    public async Task DeleteAttachmentAsync(Guid requestId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        EnsureOwner(entity);

        if (entity.Status != RequestStatus.Draft && !(entity.Status == RequestStatus.Submitted && entity.HasPendingComplementation))
        {
            throw new ConflictAppException("Somente rascunhos ou solicitações com complementação pendente permitem remover anexos.");
        }

        var attachment = entity.Attachments.SingleOrDefault(x => x.Id == attachmentId)
            ?? throw new NotFoundAppException("Anexo não encontrado.");

        _dbContext.ReimbursementAttachments.Remove(attachment);
        _dbContext.WorkflowActions.Add(new WorkflowAction(entity.Id, WorkflowActionType.AttachmentRemoved, entity.Status, entity.Status, RequireCurrentUserId(), null, attachment.OriginalFileName, _dateTimeProvider.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _attachmentStorage.DeleteAsync(attachment.StoredFileName, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AttachmentResponse>> GetAttachmentsAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        await EnsureCanReadAsync(entity, cancellationToken);
        return entity.Attachments.OrderByDescending(x => x.CreatedAt)
            .Select(x => new AttachmentResponse(x.Id, x.OriginalFileName, x.ContentType, x.SizeInBytes, x.CreatedAt))
            .ToArray();
    }

    public async Task<AttachmentDownloadResult> DownloadAttachmentAsync(Guid requestId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        await EnsureCanReadAsync(entity, cancellationToken);
        var attachment = entity.Attachments.SingleOrDefault(x => x.Id == attachmentId)
            ?? throw new NotFoundAppException("Anexo não encontrado.");

        try
        {
            var content = await _attachmentStorage.OpenReadAsync(attachment.StoredFileName, cancellationToken);
            return new AttachmentDownloadResult(content, attachment.OriginalFileName, attachment.ContentType);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundAppException("O arquivo do anexo não foi encontrado no armazenamento.", "attachment_file_missing");
        }
    }

    public async Task<IReadOnlyCollection<WorkflowActionResponse>> GetWorkflowActionsAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var entity = await LoadRequestAggregateAsync(requestId, cancellationToken);
        await EnsureCanReadAsync(entity, cancellationToken);
        return entity.WorkflowActions.OrderBy(x => x.OccurredAt)
            .Select(x => new WorkflowActionResponse(x.Id, x.ActionType, x.FromStatus, x.ToStatus, x.PerformedByUserId, x.ReasonCode, x.Comment, x.OccurredAt))
            .ToArray();
    }

    private async Task<ReimbursementRequest> LoadRequestAggregateAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return await _dbContext.ReimbursementRequests
            .Include(x => x.Category)
            .Include(x => x.CostCenter)
            .Include(x => x.CreatedByUser)
            .Include(x => x.Attachments)
            .Include(x => x.WorkflowActions)
            .Include(x => x.PaymentRecord)
            .SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new NotFoundAppException("Solicitação não encontrada.");
    }

    private async Task<ReimbursementDetailResponse> BuildDetailAsync(ReimbursementRequest entity, CancellationToken cancellationToken)
    {
        var allowedActions = await GetAllowedActionsAsync(entity, cancellationToken);
        return new ReimbursementDetailResponse(
            entity.Id,
            entity.RequestNumber,
            entity.Title,
            entity.CategoryId,
            entity.Category?.Name ?? string.Empty,
            entity.Amount,
            entity.Currency,
            entity.ExpenseDate,
            entity.Description,
            entity.CostCenterId,
            entity.CostCenter?.Code ?? string.Empty,
            entity.Status,
            entity.CreatedByUserId,
            entity.CreatedByUser?.FullName ?? string.Empty,
            entity.ApprovedByUserId,
            entity.PaidByUserId,
            entity.RejectionReason,
            entity.DecisionReasonCode,
            entity.DecisionComment,
            entity.HasPendingComplementation,
            entity.SubmittedAt,
            entity.ApprovedAt,
            entity.RejectedAt,
            entity.PaidAt,
            entity.ComplementationRequestedAt,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.RowVersion.ToString("N"),
            allowedActions,
            entity.Attachments.OrderByDescending(x => x.CreatedAt).Select(x => new AttachmentResponse(x.Id, x.OriginalFileName, x.ContentType, x.SizeInBytes, x.CreatedAt)).ToArray(),
            entity.WorkflowActions.OrderBy(x => x.OccurredAt).Select(x => new WorkflowActionResponse(x.Id, x.ActionType, x.FromStatus, x.ToStatus, x.PerformedByUserId, x.ReasonCode, x.Comment, x.OccurredAt)).ToArray());
    }

    private async Task<ReimbursementAllowedActions> GetAllowedActionsAsync(ReimbursementRequest entity, CancellationToken cancellationToken)
    {
        var userId = _currentUserContext.UserId;
        var role = _currentUserContext.Role;
        var isOwner = userId == entity.CreatedByUserId;
        var isScopedManager = role == UserRole.Manager && await IsManagerScopedAsync(entity.CostCenterId, cancellationToken);
        var canEditAfterComplementation = entity.Status == RequestStatus.Submitted && entity.HasPendingComplementation;

        return new ReimbursementAllowedActions(
            role == UserRole.Collaborator && isOwner && (entity.Status == RequestStatus.Draft || canEditAfterComplementation),
            role == UserRole.Collaborator && isOwner && (entity.Status == RequestStatus.Draft || canEditAfterComplementation),
            isScopedManager && entity.Status == RequestStatus.Submitted && !entity.HasPendingComplementation,
            isScopedManager && entity.Status == RequestStatus.Submitted,
            isScopedManager && entity.Status == RequestStatus.Submitted && !entity.HasPendingComplementation,
            role == UserRole.Finance && entity.Status == RequestStatus.Approved,
            role == UserRole.Collaborator && isOwner && (entity.Status == RequestStatus.Draft || canEditAfterComplementation),
            role == UserRole.Collaborator && isOwner && (entity.Status == RequestStatus.Draft || canEditAfterComplementation));
    }

    private async Task EnsureCanReadAsync(ReimbursementRequest entity, CancellationToken cancellationToken)
    {
        var role = _currentUserContext.Role ?? throw new UnauthorizedAppException("Usuário não autenticado.");
        var userId = RequireCurrentUserId();

        var allowed = role switch
        {
            UserRole.Collaborator => entity.CreatedByUserId == userId,
            UserRole.Manager => await IsManagerScopedAsync(entity.CostCenterId, cancellationToken),
            UserRole.Finance => true,
            UserRole.Administrator => true,
            _ => false
        };

        if (!allowed)
        {
            await _auditService.WriteAsync("authorization.denied", "reimbursement_request", entity.Id.ToString(), AuditSeverity.Warning, null, cancellationToken);
            throw new ForbiddenAppException("Acesso negado à solicitação.");
        }
    }

    private void EnsureOwner(ReimbursementRequest entity)
    {
        EnsureRole(UserRole.Collaborator);
        if (entity.CreatedByUserId != RequireCurrentUserId())
        {
            throw new ForbiddenAppException("Somente o criador pode alterar a solicitação.");
        }
    }

    private async Task EnsureManagerScopeAsync(ReimbursementRequest entity, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.Manager);
        if (!await IsManagerScopedAsync(entity.CostCenterId, cancellationToken))
        {
            throw new ForbiddenAppException("O gestor não possui escopo para este centro de custo.");
        }
    }

    private async Task<bool> IsManagerScopedAsync(Guid costCenterId, CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        return await _dbContext.ManagerCostCenterScopes.AnyAsync(x => x.ManagerId == userId && x.CostCenterId == costCenterId, cancellationToken);
    }

    private async Task EnsureRequestedCostCenterIsAllowedAsync(Guid? costCenterId, CancellationToken cancellationToken)
    {
        if (!costCenterId.HasValue || _currentUserContext.Role != UserRole.Manager)
        {
            return;
        }

        if (!await IsManagerScopedAsync(costCenterId.Value, cancellationToken))
        {
            throw new ForbiddenAppException("O gestor não possui escopo para o centro de custo informado.");
        }
    }

    private IQueryable<ReimbursementRequest> BuildScopedQueryAsync()
    {
        var role = _currentUserContext.Role ?? throw new UnauthorizedAppException("Usuário não autenticado.");
        var userId = RequireCurrentUserId();
        IQueryable<ReimbursementRequest> query = _dbContext.ReimbursementRequests.AsQueryable();

        return role switch
        {
            UserRole.Collaborator => query.Where(x => x.CreatedByUserId == userId),
            UserRole.Manager => query.Where(x => _dbContext.ManagerCostCenterScopes.Any(scope => scope.ManagerId == userId && scope.CostCenterId == x.CostCenterId)),
            UserRole.Finance => query,
            UserRole.Administrator => query,
            _ => throw new ForbiddenAppException("Papel sem acesso à listagem.")
        };
    }

    private async Task<User> GetCurrentUserEntityAsync(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        return await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAppException("Usuário não encontrado.");
    }

    private async Task<ReimbursementCategory> GetActiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return await _dbContext.ReimbursementCategories.SingleOrDefaultAsync(x => x.Id == categoryId && x.IsActive, cancellationToken)
            ?? throw Validation("Categoria inválida.", new Dictionary<string, string[]>
            {
                ["categoryId"] = ["Categoria inválida."]
            });
    }

    private void EnsureAttachmentPolicy(ReimbursementRequest entity)
    {
        if (entity.Category?.ReceiptRequiredAlways == true && entity.Attachments.Count == 0)
        {
            throw Validation("A categoria exige comprovante obrigatório.", new Dictionary<string, string[]>
            {
                ["attachments"] = ["A categoria exige comprovante obrigatório."]
            });
        }

        var threshold = entity.Category?.ReceiptRequiredAboveAmount;
        if (threshold.HasValue && entity.Amount >= threshold.Value && entity.Attachments.Count == 0)
        {
            throw Validation("É obrigatório anexar comprovante para este valor.", new Dictionary<string, string[]>
            {
                ["attachments"] = ["É obrigatório anexar comprovante para este valor."]
            });
        }
    }

    private void EnsureSubmissionDeadline(ReimbursementRequest entity)
    {
        var deadlineDays = entity.Category?.SubmissionDeadlineDays;
        if (!deadlineDays.HasValue)
        {
            return;
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime.Date);
        if (entity.ExpenseDate.AddDays(deadlineDays.Value) < today)
        {
            throw Validation("A solicitação foi enviada fora do prazo permitido para a categoria.", new Dictionary<string, string[]>
            {
                ["expenseDate"] = ["A solicitação foi enviada fora do prazo permitido para a categoria."]
            });
        }
    }

    private void ValidateCategoryPolicy(ReimbursementCategory category, decimal amount)
    {
        if (category.MaxAmount.HasValue && amount > category.MaxAmount.Value)
        {
            throw Validation("O valor ultrapassa o limite permitido para a categoria.", new Dictionary<string, string[]>
            {
                ["amount"] = ["O valor ultrapassa o limite permitido para a categoria."]
            });
        }
    }

    private void ValidateDraftPayload(string title, decimal amount, string currency, string description, DateOnly expenseDate)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["O título é obrigatório."];
        }

        if (amount <= 0)
        {
            errors["amount"] = ["O valor deve ser maior que zero."];
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            errors["currency"] = ["A moeda deve ter três caracteres."];
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors["description"] = ["A descrição é obrigatória."];
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime.Date);
        if (expenseDate > today)
        {
            errors["expenseDate"] = ["A data da despesa não pode ser futura."];
        }

        if (errors.Count > 0)
        {
            throw Validation("A solicitação contém dados inválidos.", errors);
        }
    }

    private void ValidateRequiredDecisionReason(DecisionReasonCode reasonCode, string? comment, string fieldPrefix, IReadOnlyCollection<DecisionReasonCode>? allowedReasons = null)
    {
        var errors = new Dictionary<string, string[]>();

        if (allowedReasons is not null && !allowedReasons.Contains(reasonCode))
        {
            errors["reasonCode"] = ["O motivo informado não é permitido para esta ação."];
        }

        if (reasonCode == DecisionReasonCode.Other && string.IsNullOrWhiteSpace(comment))
        {
            errors["comment"] = ["O comentário é obrigatório quando o motivo for Outro."];
        }

        if (errors.Count > 0)
        {
            throw Validation($"A ação de {fieldPrefix} contém dados inválidos.", errors);
        }
    }

    private void ValidateApprovalDecision(DecisionReasonCode? reasonCode, string? comment)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(comment))
        {
            errors["comment"] = ["A justificativa da aprovação é obrigatória."];
        }

        if (reasonCode == DecisionReasonCode.Other && string.IsNullOrWhiteSpace(comment))
        {
            errors["comment"] = ["O comentário é obrigatório quando o motivo for Outro."];
        }

        if (errors.Count > 0)
        {
            throw Validation("A ação de aprovação contém dados inválidos.", errors);
        }
    }

    private void ValidateOptionalDecisionReason(DecisionReasonCode? reasonCode, string? comment, string fieldPrefix)
    {
        if (!reasonCode.HasValue)
        {
            return;
        }

        var errors = new Dictionary<string, string[]>();
        if (reasonCode == DecisionReasonCode.Other && string.IsNullOrWhiteSpace(comment))
        {
            errors["comment"] = ["O comentário é obrigatório quando o motivo for Outro."];
        }

        if (errors.Count > 0)
        {
            throw Validation($"A ação de {fieldPrefix} contém dados inválidos.", errors);
        }
    }

    private void ValidateAttachment(string fileName, string contentType, long sizeInBytes)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors["file"] = ["O nome do arquivo é obrigatório."];
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            errors["contentType"] = ["Tipo de arquivo não permitido."];
        }

        if (sizeInBytes <= 0 || sizeInBytes > _storageOptions.MaxFileSizeInBytes)
        {
            errors["sizeInBytes"] = [$"O arquivo deve ter até {_storageOptions.MaxFileSizeInBytes / (1024 * 1024)} MB."];
        }

        if (errors.Count > 0)
        {
            throw Validation("O anexo é inválido.", errors);
        }
    }

    private void EnsureRowVersion(ReimbursementRequest entity, string rowVersion)
    {
        if (!Guid.TryParseExact(rowVersion, "N", out var parsed) || parsed != entity.RowVersion)
        {
            throw new ConflictAppException("A solicitação foi alterada por outro usuário.", "concurrency_conflict");
        }
    }

    private Guid RequireCurrentUserId()
    {
        return _currentUserContext.UserId ?? throw new UnauthorizedAppException("Usuário não autenticado.");
    }

    private void EnsureRole(UserRole expectedRole)
    {
        if (_currentUserContext.Role != expectedRole)
        {
            throw new ForbiddenAppException("O usuário não possui permissão para executar esta ação.");
        }
    }

    private static string GenerateRequestNumber(DateTimeOffset now)
    {
        return $"RMB-{now:yyyyMMdd}-{RandomNumberGenerator.GetHexString(3)}";
    }

    private static string? NormalizeOptionalComment(string? comment)
    {
        return string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }

    private static string BuildDecisionComment(DecisionReasonCode reasonCode, string? comment)
    {
        return string.IsNullOrWhiteSpace(comment) ? GetDecisionReasonLabel(reasonCode) : comment.Trim();
    }

    private static string GetDecisionReasonLabel(DecisionReasonCode reasonCode)
    {
        return reasonCode switch
        {
            DecisionReasonCode.MissingReceipt => "Comprovante obrigatório ausente.",
            DecisionReasonCode.InvalidReceipt => "Comprovante inválido ou ilegível.",
            DecisionReasonCode.OutOfPolicy => "Despesa fora da política de reembolso.",
            DecisionReasonCode.OutOfDeadline => "Solicitação enviada fora do prazo permitido.",
            DecisionReasonCode.CategoryMismatch => "Categoria incompatível com a despesa informada.",
            DecisionReasonCode.DuplicateRequest => "Possível duplicidade de solicitação.",
            DecisionReasonCode.InconsistentAmount => "Valor inconsistente com os dados apresentados.",
            DecisionReasonCode.FraudSuspicion => "Há indícios de inconsistência relevante ou possível fraude.",
            DecisionReasonCode.NeedMoreDetails => "É necessário complementar os detalhes da despesa.",
            DecisionReasonCode.NeedAdditionalDocument => "É necessário anexar documentação complementar.",
            DecisionReasonCode.Other => "Outro motivo informado.",
            _ => "Motivo operacional registrado."
        };
    }

    private static IEnumerable<ReimbursementListItemResponse> ApplyOrdering(IEnumerable<ReimbursementListItemResponse> query, string sort)
    {
        var parts = sort.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var field = parts.ElementAtOrDefault(0)?.ToLowerInvariant() ?? "createdat";
        var direction = parts.ElementAtOrDefault(1)?.ToLowerInvariant() ?? "desc";
        var desc = direction != "asc";

        return field switch
        {
            "amount" => desc ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            "expensedate" => desc ? query.OrderByDescending(x => x.ExpenseDate) : query.OrderBy(x => x.ExpenseDate),
            "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
    }

    private ValidationAppException Validation(string message, IReadOnlyDictionary<string, string[]> errors)
        => new(message, errors);

    private static void ExecuteDomainTransition(Action action)
    {
        try
        {
            action();
        }
        catch (DomainRuleException exception)
        {
            throw new ConflictAppException(exception.Message, "invalid_workflow_transition");
        }
    }

    private async Task SaveChangesHandlingConcurrencyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictAppException("A solicitação foi alterada por outro usuário.", "concurrency_conflict");
        }
        catch (DomainRuleException exception)
        {
            throw new ConflictAppException(exception.Message, "invalid_workflow_transition");
        }
    }
}
