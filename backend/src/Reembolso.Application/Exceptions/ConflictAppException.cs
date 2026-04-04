namespace Reembolso.Application.Exceptions;

public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string message, string errorCode = "conflict")
        : base(409, errorCode, message)
    {
    }
}
