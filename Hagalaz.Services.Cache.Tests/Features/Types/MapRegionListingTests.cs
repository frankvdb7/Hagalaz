using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Features.Types;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Hagalaz.Services.Cache.Security;
using Hagalaz.TestSupport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public sealed class MapRegionListingTests
{
    private static MemoryFileStore Store(ReferenceTableFlags flags, params (int FileId, string Name)[] files)
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 0, flags, files.Length);
        foreach (var (fileId, name) in files)
        {
            var entry = new ReferenceTableEntry(fileId) { Id = ReferenceTable.CalculateId(name) };
            entry.InitializeEntries(0);
            table.AddEntry(fileId, entry);
            // Deliberately undecodable payloads: listing must read only reference metadata.
            store.Set(5, fileId, [255]);
        }
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var container = new Container(new MemoryStream(encoded.ToArray()));
        store.Set(255, 5, container.Encode());
        return store;
    }

    [TestMethod]
    public async Task ListRegions_PaginatesSparseRegionPairsWithoutReadingPayloads()
    {
        var store = Store(ReferenceTableFlags.Identifiers, (3, "l1_2"), (7, "m0_0"), (12, "m1_2"), (13, "l255_255"), (19, "unrelated"));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var first = await client.GetFromJsonAsync<GetMapRegionsDto>("/api/v1/cache/types/maps?limit=2");
        Assert.IsNotNull(first);
        Assert.AreEqual(3, first.Total);
        Assert.AreEqual(0, first.Offset);
        Assert.AreEqual(2, first.Limit);
        CollectionAssert.AreEqual(new[] { new MapRegionSummaryDto(0, 0, 0, 7, null), new MapRegionSummaryDto(258, 1, 2, 12, 3) }, first.Items.ToArray());
        var last = await client.GetFromJsonAsync<GetMapRegionsDto>("/api/v1/cache/types/maps?offset=2&limit=2");
        Assert.IsNotNull(last);
        Assert.AreEqual(3, last.Total);
        CollectionAssert.AreEqual(new[] { new MapRegionSummaryDto(65535, 255, 255, null, 13) }, last.Items.ToArray());
        var beyond = await client.GetFromJsonAsync<GetMapRegionsDto>("/api/v1/cache/types/maps?offset=65536&limit=200");
        Assert.IsNotNull(beyond);
        Assert.IsEmpty(beyond.Items);
        Assert.AreEqual(3, beyond.Total);
        foreach (var fileId in new[] { 3, 7, 12, 13, 19 }) Assert.AreEqual(0, store.GetReadCount(5, fileId));
    }

    [TestMethod]
    public async Task ListRegions_DistinguishesEmptyIndexFromMissingNames()
    {
        await using var emptyApp = await CacheServiceTestHost.StartAsync(Store(ReferenceTableFlags.Identifiers), group => group.MapTypeEndpoints());
        using var emptyClient = CacheServiceTestHost.CreateClient(emptyApp);
        var empty = await emptyClient.GetFromJsonAsync<GetMapRegionsDto>("/api/v1/cache/types/maps");
        Assert.IsNotNull(empty);
        Assert.AreEqual(0, empty.Total);
        Assert.IsEmpty(empty.Items);
        await using var unnamedApp = await CacheServiceTestHost.StartAsync(Store(0, (2, "m1_2")), group => group.MapTypeEndpoints());
        using var unnamedClient = CacheServiceTestHost.CreateClient(unnamedApp);
        using var response = await unnamedClient.GetAsync("/api/v1/cache/types/maps");
        Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "named reference metadata");
    }

    [TestMethod]
    [DataRow(-1, 10)]
    [DataRow(65537, 10)]
    [DataRow(0, 0)]
    [DataRow(0, 201)]
    public async Task ListRegions_RejectsInvalidPagination(int offset, int limit)
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync($"/api/v1/cache/types/maps?offset={offset}&limit={limit}");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ListRegions_InheritsCacheAuthorizationPolicy()
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapCacheEndpoints());
        var endpoint = app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText!.EndsWith("/types/maps", StringComparison.Ordinal));
        Assert.IsTrue(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(policy => policy.Policy == CacheAuthorization.PolicyName));
    }
}
