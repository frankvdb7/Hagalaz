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
using Hagalaz.Services.Cache.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features;

public static class CacheEndpoints
{
    public static IEndpointRouteBuilder MapCacheEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cache")
            .WithTags("Cache")
            .RequireAuthorization(CacheAuthorization.PolicyName);

        group.MapFileCountEndpoints();
        group.MapFileLookupEndpoints();
        group.MapReferenceTableEndpoints();
        group.MapContainerEndpoints();
        group.MapStoreMutationEndpoints();
        group.MapArchiveEndpoints();
        group.MapRawFileEndpoints();
        group.MapChecksumEndpoints();
        group.MapTypeEndpoints();
        group.MapModelEndpoints();
        group.MapTextureEndpoints();

        return app;
    }
}
