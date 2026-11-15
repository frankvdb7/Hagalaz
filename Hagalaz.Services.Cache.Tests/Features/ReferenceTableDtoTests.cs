using System.Net.Http.Json;
using System.Text.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.ReferenceTables;
using Hagalaz.Services.Cache.Features.ReferenceTables.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class ReferenceTableDtoTests
{
    [TestMethod]
    public async Task ReferenceDto_RetainsSparseIdsFlagsDigestsAndChildMetadata()
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 42, ReferenceTableFlags.Identifiers | ReferenceTableFlags.Digests, 2);
        var digest = Enumerable.Range(0, 64).Select(value => (byte)value).ToArray();
        var first = new ReferenceTableEntry(0) { Id = 1234, Crc32 = -123456, Version = 5, WhirlpoolDigest = digest };
        first.InitializeEntries(2);
        first.AddEntry(2, new ReferenceTableChildEntry(0) { Id = 445 });
        first.AddEntry(9, new ReferenceTableChildEntry(1) { Id = 446 });
        table.AddEntry(1, first);
        var second = new ReferenceTableEntry(1) { Id = 5678, Version = 6, WhirlpoolDigest = new byte[64] };
        second.InitializeEntries(0);
        table.AddEntry(100, second);
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var container = new Container(new MemoryStream(encoded.ToArray()));
        store.Set(255, 7, container.Encode());
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapReferenceTableEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync("/api/v1/cache/indexes/7/reference-table");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetReferenceTableDto>();
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Version);
        Assert.AreEqual((byte)7, result.Protocol);
        Assert.AreEqual((byte)3, result.Flags);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(101, result.Capacity);
        CollectionAssert.AreEqual(new[] { 1, 100 }, result.Entries.Select(entry => entry.FileId).ToArray());
        var entry = result.Entries[0];
        Assert.AreEqual(1234, entry.Id);
        Assert.AreEqual(-123456, entry.Crc32);
        Assert.AreEqual(5, entry.Version);
        Assert.AreEqual(2, entry.Capacity);
        Assert.AreEqual(Convert.ToBase64String(digest), entry.WhirlpoolDigestBase64);
        CollectionAssert.AreEqual(new[] { new ReferenceTableChildEntryDto(2, 445, 0), new ReferenceTableChildEntryDto(9, 446, 1) }, entry.Children.ToArray());
        Assert.AreEqual(0, result.Entries[1].Capacity);
        Assert.HasCount(0, result.Entries[1].Children);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        CollectionAssert.AreEquivalent(new[] { "version", "protocol", "flags", "capacity", "entries", "count" },
            json.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.AreEqual(JsonValueKind.Number, json.RootElement.GetProperty("flags").ValueKind);
    }
}
