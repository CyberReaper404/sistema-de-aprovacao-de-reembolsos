namespace Reembolso.Application.Exceptions;

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message, string errorCode = "unauthorized")
        : base(401, errorCode, message)
    {
    }
}
