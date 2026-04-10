namespace Reembolso.Domain.Enums;

public enum DecisionReasonCode
{
    MissingReceipt = 1,
    InvalidReceipt = 2,
    OutOfPolicy = 3,
    OutOfDeadline = 4,
    CategoryMismatch = 5,
    DuplicateRequest = 6,
    InconsistentAmount = 7,
    FraudSuspicion = 8,
    NeedMoreDetails = 9,
    NeedAdditionalDocument = 10,
    Other = 11
}
