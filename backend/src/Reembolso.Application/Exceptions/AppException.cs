namespace Reembolso.Application.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(int statusCode, string errorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }

    public string ErrorCode { get; }
}

