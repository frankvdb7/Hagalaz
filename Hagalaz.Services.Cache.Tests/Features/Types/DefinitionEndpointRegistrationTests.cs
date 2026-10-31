using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Features.Types;
using Hagalaz.TestSupport;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public class DefinitionEndpointRegistrationTests
{
    [TestMethod]
    public async Task DefinitionRegistrations_PreserveEveryExistingMethodAndRoute()
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapTypeEndpoints());
        var routes = app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods
                .Select(method => method + " " + endpoint.RoutePattern.RawText))
            .ToArray();
        string[] expected =
        [
            "GET /api/v1/cache/types/animations/range",
            "GET /api/v1/cache/types/animations/{id:int}",
            "GET /api/v1/cache/types/archive-sizes",
            "GET /api/v1/cache/types/client-map-definitions/range",
            "GET /api/v1/cache/types/client-map-definitions/{id:int}",
            "GET /api/v1/cache/types/config-definitions/range",
            "GET /api/v1/cache/types/config-definitions/{id:int}",
            "GET /api/v1/cache/types/cs2-int/{id:int}",
            "GET /api/v1/cache/types/cs2/range",
            "GET /api/v1/cache/types/cs2/{id:int}",
            "GET /api/v1/cache/types/equipment-defaults",
            "GET /api/v1/cache/types/graphics/range",
            "GET /api/v1/cache/types/graphics/{id:int}",
            "GET /api/v1/cache/types/huffman/coding",
            "GET /api/v1/cache/types/items/range",
            "GET /api/v1/cache/types/items/search",
            "GET /api/v1/cache/types/items/{id:int}",
            "GET /api/v1/cache/types/items/{id:int}/definition",
            "GET /api/v1/cache/types/items/{id:int}/image",
            "GET /api/v1/cache/types/maps/{id:int}",
            "GET /api/v1/cache/types/npcs/range",
            "GET /api/v1/cache/types/npcs/search",
            "GET /api/v1/cache/types/npcs/{id:int}",
            "GET /api/v1/cache/types/objects/range",
            "GET /api/v1/cache/types/objects/{id:int}",
            "GET /api/v1/cache/types/quests/range",
            "GET /api/v1/cache/types/quests/{id:int}",
            "GET /api/v1/cache/types/sprites/range",
            "GET /api/v1/cache/types/sprites/search",
            "GET /api/v1/cache/types/sprites/{id:int}",
            "GET /api/v1/cache/types/sprites/{id:int}/image",
            "GET /api/v1/cache/types/varp-bits/range",
            "GET /api/v1/cache/types/varp-bits/{id:int}",
            "POST /api/v1/cache/types/config-definitions/{id:int}",
            "POST /api/v1/cache/types/items/{id:int}/duplicate",
            "POST /api/v1/cache/types/maps/{id:int}/decode",
            "POST /api/v1/cache/types/npcs/{id:int}/combat-level",
            "POST /api/v1/cache/types/objects/{id:int}/name",
            "POST /api/v1/cache/types/sprites/{id:int}/image",
            "POST /api/v1/cache/types/varp-bits/{id:int}",
            "PUT /api/v1/cache/types/items/{id:int}/definition",
        ];
        CollectionAssert.AreEquivalent(expected, routes);
    }
}
