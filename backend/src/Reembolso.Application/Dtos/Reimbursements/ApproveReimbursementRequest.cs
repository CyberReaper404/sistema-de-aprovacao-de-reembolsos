using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Reimbursements;

public sealed record ApproveReimbursementRequest(DecisionReasonCode? ReasonCode, string? Comment);
