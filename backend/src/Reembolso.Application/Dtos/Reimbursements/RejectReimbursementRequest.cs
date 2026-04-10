using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Reimbursements;

public sealed record RejectReimbursementRequest(DecisionReasonCode ReasonCode, string? Comment);
