namespace Reembolso.Application.Exceptions;

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string message, string errorCode = "not_found")
        : base(404, errorCode, message)
    {
    }
}
