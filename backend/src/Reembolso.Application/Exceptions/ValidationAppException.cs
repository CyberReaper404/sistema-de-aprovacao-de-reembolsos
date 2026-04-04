namespace Reembolso.Application.Exceptions;

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(string message, IReadOnlyDictionary<string, string[]> errors, string errorCode = "validation_error")
        : base(400, errorCode, message)
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
