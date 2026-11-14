using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Hagalaz.Services.Cache.Features.Models;

/// <summary>Serves a rendered PNG, checking revalidation first so unchanged images cost only reference-table lookups.</summary>
internal static class RenderedImageResponse
{
    public static IResult Create(HttpContext context, string tag, Func<byte[]?> render)
    {
        var etag = new EntityTagHeaderValue($"\"{tag}\"");
        var validators = context.Request.GetTypedHeaders().IfNoneMatch;
        if (validators?.Any(candidate => candidate.Equals(EntityTagHeaderValue.Any) || candidate.Compare(etag, useStrongComparison: false)) == true)
        {
            SetValidator(context, etag);
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        byte[]? png;
        try { png = render(); }
        catch (FileNotFoundException) { png = null; }
        if (png is null) return TypedResults.NotFound();

        SetValidator(context, etag);
        return TypedResults.File(png, "image/png");
    }

    private static void SetValidator(HttpContext context, EntityTagHeaderValue etag)
    {
        context.Response.Headers.CacheControl = "private, no-cache";
        context.Response.GetTypedHeaders().ETag = etag;
    }
}
