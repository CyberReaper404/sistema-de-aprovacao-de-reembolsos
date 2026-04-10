using Reembolso.Domain.Enums;

namespace Reembolso.Application.Dtos.Reimbursements;

public sealed record RequestComplementationRequest(DecisionReasonCode ReasonCode, string? Comment);
