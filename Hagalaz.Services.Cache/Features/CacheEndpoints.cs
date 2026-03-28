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

        return app;
    }
}
