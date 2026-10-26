using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Hagalaz.Services.Cache.Features.Shared;

internal sealed class CacheExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        await EndpointErrors.MapException(exception).ExecuteAsync(httpContext);
        return true;
    }
}
