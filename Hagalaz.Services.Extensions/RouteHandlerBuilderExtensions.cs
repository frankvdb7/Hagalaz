using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Hagalaz.Services.Extensions;

public static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder WithCacheControl(this RouteHandlerBuilder builder, TimeSpan maxAge, bool isPublic = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAge, TimeSpan.Zero);

        var cacheControl = new CacheControlHeaderValue
        {
            MaxAge = maxAge,
            Public = isPublic,
            Private = !isPublic
        }.ToString();

        return builder.AddEndpointFilter((context, next) =>
        {
            var response = context.HttpContext.Response;
            response.OnStarting(() =>
            {
                var cacheableStatus = response.StatusCode is
                    StatusCodes.Status200OK or
                    StatusCodes.Status206PartialContent or
                    StatusCodes.Status304NotModified;

                if (cacheableStatus
                    && !response.Headers.ContainsKey(HeaderNames.CacheControl)
                    && !response.Headers.ContainsKey(HeaderNames.SetCookie))
                {
                    response.Headers.CacheControl = cacheControl;
                }

                return Task.CompletedTask;
            });

            return next(context);
        });
    }
}