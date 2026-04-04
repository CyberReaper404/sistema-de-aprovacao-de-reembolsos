namespace Reembolso.Application.Exceptions;

public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message, string errorCode = "forbidden")
        : base(403, errorCode, message)
    {
    }
}
