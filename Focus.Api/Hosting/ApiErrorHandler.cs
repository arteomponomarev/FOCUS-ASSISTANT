using Focus.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Focus.Api.Hosting;

internal sealed class ApiErrorHandler(ILogger<ApiErrorHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            return false;

        var failure = exception as UseCaseException;
        var status = failure?.Failure switch
        {
            Failure.Invalid => 400,
            Failure.Unauthenticated => 401,
            Failure.Forbidden => 403,
            Failure.NotFound => 404,
            Failure.Conflict => 409,
            Failure.Unprocessable => 422,
            _ => 500
        };

        if (status == 500)
            logger.LogError("Request failed with exception type {ExceptionType}", exception.GetType().FullName);
        else if (status == 401)
        {
            logger.LogWarning("Authentication request was rejected");
            context.Response.Headers.WWWAuthenticate = "Bearer";
        }

        await Results.Problem(statusCode: status, detail: failure?.Message).ExecuteAsync(context);
        return true;
    }
}
