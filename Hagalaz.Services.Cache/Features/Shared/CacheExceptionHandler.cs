using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.Cache.Features.Shared;

internal sealed class CacheExceptionHandler(ILogger<CacheExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var result = EndpointErrors.MapException(exception);
        var statusCode = (result as IStatusCodeHttpResult)?.StatusCode ?? StatusCodes.Status500InternalServerError;
        // Handling the exception suppresses the diagnostics middleware's own logging, so log it here by severity.
        var level = statusCode switch
        {
            >= StatusCodes.Status500InternalServerError => LogLevel.Error,
            StatusCodes.Status422UnprocessableEntity => LogLevel.Warning,
            _ => LogLevel.Debug
        };
        logger.Log(level, exception, "Cache request {Method} {Path} failed with {StatusCode}.",
            httpContext.Request.Method, httpContext.Request.Path, statusCode);
        await result.ExecuteAsync(httpContext);
        return true;
    }
}
