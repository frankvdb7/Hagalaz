using Hagalaz.Services.Cache.Features.Archives;
using Hagalaz.Services.Cache.Features.Checksums;
using Hagalaz.Services.Cache.Features.Containers;
using Hagalaz.Services.Cache.Features.FileCounts;
using Hagalaz.Services.Cache.Features.FileLookup;
using Hagalaz.Services.Cache.Features.Models;
using Hagalaz.Services.Cache.Features.Mutations;
using Hagalaz.Services.Cache.Features.RawFiles;
using Hagalaz.Services.Cache.Features.ReferenceTables;
using Hagalaz.Services.Cache.Features.Types;
using Hagalaz.Services.Cache.Features.Widgets;
using Hagalaz.Services.Cache.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features;

public static class CacheEndpoints
{
    public static IEndpointRouteBuilder MapCacheEndpoints(this IEndpointRouteBuilder app)
    {
        // Every route can fail validation, miss a cache file, meet unreadable cache data or be refused by the policy.
        // Validation and missing-file problems are written as application/json; mapped 422/500 problems as problem+json.
        var group = app.MapGroup("/api/v1/cache")
            .WithTags("Cache")
            .RequireAuthorization(CacheAuthorization.PolicyName)
            // Produces() exists only on route handlers, so the group carries the same metadata directly.
            .WithMetadata(
                new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(ProblemDetails), ["application/json"]),
                new ProducesResponseTypeMetadata(StatusCodes.Status401Unauthorized, typeof(void)),
                new ProducesResponseTypeMetadata(StatusCodes.Status403Forbidden, typeof(void)),
                new ProducesResponseTypeMetadata(StatusCodes.Status404NotFound, typeof(ProblemDetails), ["application/json"]),
                new ProducesResponseTypeMetadata(StatusCodes.Status422UnprocessableEntity, typeof(ProblemDetails), ["application/problem+json"]),
                new ProducesResponseTypeMetadata(StatusCodes.Status500InternalServerError, typeof(ProblemDetails), ["application/problem+json"]));

        group.MapFileCountEndpoints();
        group.MapFileLookupEndpoints();
        group.MapReferenceTableEndpoints();
        group.MapContainerEndpoints();
        group.MapStoreMutationEndpoints();
        group.MapArchiveEndpoints();
        group.MapRawFileEndpoints();
        group.MapChecksumEndpoints();
        group.MapTypeEndpoints();
        group.MapWidgetComponentEndpoints();
        group.MapWidgetPrograms();
        group.MapModelEndpoints();
        group.MapTextureEndpoints();

        return app;
    }
}
