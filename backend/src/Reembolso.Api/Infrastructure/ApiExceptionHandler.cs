using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Reembolso.Application.Exceptions;

namespace Reembolso.Api.Infrastructure;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path,
            Detail = exception.Message,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        };

        if (exception is ValidationAppException validationException)
        {
            problemDetails.Status = validationException.StatusCode;
            problemDetails.Title = "A requisição é inválida.";
            problemDetails.Extensions["errorCode"] = validationException.ErrorCode;
            problemDetails.Extensions["errors"] = validationException.Errors;
        }
        else if (exception is AppException appException)
        {
            problemDetails.Status = appException.StatusCode;
            problemDetails.Title = "Falha na operação.";
            problemDetails.Extensions["errorCode"] = appException.ErrorCode;
        }
        else
        {
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Title = "Erro interno.";
            problemDetails.Extensions["errorCode"] = "internal_error";
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
