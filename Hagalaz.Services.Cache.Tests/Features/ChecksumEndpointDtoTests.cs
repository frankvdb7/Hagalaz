using System.Net.Http.Json;
using System.Text.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Utilities;
using Hagalaz.Security;
using Hagalaz.Services.Cache.Features.Checksums;
using Hagalaz.Services.Cache.Features.Checksums.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class ChecksumEndpointDtoTests
{
    [TestMethod]
    public async Task ReadChecksumTable_RetainsOrderValuesAndBase64ForPopulatedAndEmptyIndexes()
    {
        var store = new MemoryFileStore();
        for (var index = 0; index < store.IndexFileCount; index++) store.Set(255, index, []);
        var table = new ReferenceTable(7, 37, (ReferenceTableFlags)0, 1);
        var archive = new ReferenceTableEntry(0);
        archive.InitializeEntries(1);
        archive.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(0, archive);
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var container = new Container(new MemoryStream(encoded.ToArray()));
        var bytes = container.Encode();
        store.Set(255, 7, bytes);

        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapChecksumEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync("/api/v1/cache/checksum-table");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetChecksumTableDto>();
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(Enumerable.Range(0, store.IndexFileCount).ToArray(), result.Entries.Select(entry => entry.IndexId).ToArray());
        Assert.AreEqual(new ChecksumTableEntryDto(7, BufferUtilities.GetCrcChecksum(bytes, 0, bytes.Length), 37,
            Convert.ToBase64String(Whirlpool.GenerateDigest(bytes, 0, bytes.Length))), result.Entries[7]);
        Assert.AreEqual(new ChecksumTableEntryDto(0, 0, 0, Convert.ToBase64String(new byte[64])), result.Entries[0]);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        CollectionAssert.AreEquivalent(new[] { "indexId", "crc32", "version", "digestBase64" },
            json.RootElement.GetProperty("entries")[7].EnumerateObject().Select(property => property.Name).ToArray());
    }
}
