using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features;
using Hagalaz.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public sealed class SpriteTypeSearchTests
{
    [TestMethod]
    public async Task SearchSprites_EmptyQueryLoadsOnlyRequestedPage()
    {
        int[] spriteIds = [0, 3, 100];
        var provider = new TrackingSpriteProvider(spriteIds);
        var store = CreateStore(spriteIds);

        await using var app = await CacheServiceTestHost.StartAsync(
            store,
            cacheGroup => cacheGroup.MapTypeEndpoints(),
            services => services.AddSingleton<ITypeProvider<ISpriteType>>(provider));
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.GetAsync("/api/v1/cache/types/sprites/search?query=&offset=1&limit=1");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TypeEndpoints.SearchResponse<TypeEndpoints.SpriteTypeResponse>>();
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result.Query);
        Assert.AreEqual(3, result.TotalMatches);
        Assert.AreEqual(1, result.Offset);
        Assert.AreEqual(1, result.Items.Count);
        Assert.AreEqual(3, result.Items[0].Id);
        CollectionAssert.AreEqual(new[] { 3 }, provider.RequestedIds.ToArray());
    }

    [TestMethod]
    public async Task SearchSprites_SubstringOffsetPagesSparseIdsBeforeLoadingSprites()
    {
        int[] spriteIds = [1, 10, 11, 22, 101, 120];
        var provider = new TrackingSpriteProvider(spriteIds);
        var store = CreateStore(spriteIds);

        await using var app = await CacheServiceTestHost.StartAsync(
            store,
            cacheGroup => cacheGroup.MapTypeEndpoints(),
            services => services.AddSingleton<ITypeProvider<ISpriteType>>(provider));
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.GetAsync("/api/v1/cache/types/sprites/search?query=1&offset=2&limit=2");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TypeEndpoints.SearchResponse<TypeEndpoints.SpriteTypeResponse>>();
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.TotalMatches);
        CollectionAssert.AreEqual(new[] { 11, 101 }, result.Items.Select(item => item.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 11, 101 }, provider.RequestedIds.ToArray());
    }

    private static MemoryFileStore CreateStore(int[] spriteIds)
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, spriteIds.Length);
        for (var i = 0; i < spriteIds.Length; i++)
        {
            var archive = new ReferenceTableEntry(i);
            archive.InitializeEntries(1);
            archive.AddEntry(0, new ReferenceTableChildEntry(0));
            table.AddEntry(spriteIds[i], archive);
        }

        var store = new MemoryFileStore();
        using var encodedTable = new ReferenceTableCodec().Encode(table);
        store.Set(255, 8, EncodeContainer(encodedTable.ToArray()));
        return store;
    }

    private static byte[] EncodeContainer(byte[] data)
    {
        using var container = new Container(new MemoryStream(data));
        return container.Encode();
    }

    private sealed class TrackingSpriteProvider(int[] spriteIds) : ITypeProvider<ISpriteType>
    {
        private readonly HashSet<int> _spriteIds = [.. spriteIds];

        public int ArchiveSize => spriteIds.Max() + 1;

        public List<int> RequestedIds { get; } = [];

        public ISpriteType Get(int typeId)
        {
            RequestedIds.Add(typeId);
            if (!_spriteIds.Contains(typeId))
            {
                throw new FileNotFoundException($"Sprite {typeId} does not exist.");
            }

            return new SpriteType(typeId);
        }

        public ISpriteType[] GetAll() => GetRange(0, ArchiveSize);

        public ISpriteType[] GetRange(int startTypeId, int endTypeId) =>
            Enumerable.Range(startTypeId, endTypeId - startTypeId).Select(Get).ToArray();
    }
}
