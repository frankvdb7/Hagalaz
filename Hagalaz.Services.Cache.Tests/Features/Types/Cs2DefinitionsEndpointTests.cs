using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.Types;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public sealed class Cs2DefinitionsEndpointTests
{
    private const string Route = "/api/v1/cache/types/cs2";
    private const string Script = "4100003F800000000000000100000001000100020003000400050006010001FFFFFFFF00000002000B";
    private const string Script279 = "00025E00000001025E03D1001F03860000020000000004000000000000000000000000000001";
    private const string Caller = "00" + "025E0000002A" + "00914100" + "003F8000000000000001" +
        "000300000007" + "000300000008" + "000300000004" + "000200" + "00000007000000000000000000000000000001";
    private const string Callee = "00" + "000200" + "00000001000100010001000100010001000001";

    [TestMethod]
    public async Task Range_SkipsMissingIdsAndReturnsSummaries()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var rows = await client.GetFromJsonAsync<JsonElement>($"{Route}/range?startId=0&endIdExclusive=4");

        CollectionAssert.AreEqual(new[] { 1, 3 }, rows.EnumerateArray().Select(row => row.GetProperty("id").GetInt32()).ToArray());
        Assert.AreEqual(1, rows[0].GetProperty("opcodeCount").GetInt32());
        Assert.IsFalse(rows[0].TryGetProperty("instructions", out _));
    }

    [TestMethod]
    public async Task Detail_ReturnsSummaryAndExactLongOperand()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var script = await client.GetFromJsonAsync<JsonElement>($"{Route}/1");

        Assert.AreEqual("A", script.GetProperty("name").GetString());
        Assert.AreEqual(1, script.GetProperty("intLocalsCount").GetInt32());
        var instruction = script.GetProperty("instructions")[0];
        Assert.AreEqual(0, instruction.GetProperty("index").GetInt32());
        Assert.AreEqual(63, instruction.GetProperty("opcode").GetInt32());
        Assert.AreEqual("long", instruction.GetProperty("operandKind").GetString());
        Assert.AreEqual("-9223372036854775807", instruction.GetProperty("operand").GetString());
        var executable = script.GetProperty("executableSource");
        Assert.AreEqual("L0: pushLong \"-9223372036854775807\"", executable[0].GetString());
        Assert.AreEqual("push_long(-9223372036854775807)", script.GetProperty("source")[0].GetProperty("text").GetString());
        var switchCase = script.GetProperty("switchTables")[0][0];
        Assert.AreEqual(-1, switchCase.GetProperty("key").GetInt32());
        Assert.AreEqual(2, switchCase.GetProperty("offset").GetInt32());
    }

    [TestMethod]
    public async Task Detail_ReturnsServerSourceAndWidgetLinkForPinnedScript279()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var script = await client.GetFromJsonAsync<JsonElement>($"{Route}/279");

        var source = script.GetProperty("source");
        Assert.AreEqual(2, source.GetArrayLength());
        Assert.AreEqual("setHidden(widget(977, 31), true)", source[0].GetProperty("text").GetString());
        Assert.AreEqual(977, source[0].GetProperty("widget").GetProperty("id").GetInt32());
        Assert.AreEqual(31, source[0].GetProperty("widget").GetProperty("component").GetInt32());
        Assert.AreEqual("return", source[1].GetProperty("text").GetString());
    }

    [TestMethod]
    public async Task Detail_ResolvesCallArgumentsFromCacheAndKeepsUnavailableCalleesPartial()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var script = await client.GetFromJsonAsync<JsonElement>($"{Route}/5");
        var source = script.GetProperty("source");

        Assert.AreEqual("call script_7(42, \"A\", -9223372036854775807)", source[0].GetProperty("text").GetString());
        Assert.IsFalse(source[0].GetProperty("raw").GetBoolean());
        Assert.AreEqual("call script_8(?)  // opcode 3 int 8", source[1].GetProperty("text").GetString());
        Assert.IsTrue(source[1].GetProperty("raw").GetBoolean());
        Assert.AreEqual("call script_4(?)  // opcode 3 int 4", source[2].GetProperty("text").GetString());
        Assert.IsTrue(source[2].GetProperty("raw").GetBoolean());
        Assert.AreEqual("return", source[3].GetProperty("text").GetString());
    }

    [TestMethod]
    [DataRow(2, HttpStatusCode.NotFound)]
    [DataRow(4, HttpStatusCode.UnprocessableEntity)]
    public async Task Detail_DistinguishesMissingAndMalformedScripts(int id, HttpStatusCode statusCode)
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync($"{Route}/{id}");

        Assert.AreEqual(statusCode, response.StatusCode);
    }

    private static MemoryFileStore Store()
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 1, 0, 6);
        foreach (var (id, bytes) in new[] { (1, Script), (3, Script), (4, "FF"), (5, Caller), (7, Callee), (279, Script279) })
        {
            var entry = new ReferenceTableEntry(id);
            entry.InitializeEntries(1);
            entry.AddEntry(0, new ReferenceTableChildEntry(0));
            table.AddEntry(id, entry);
            using var script = new Container(new MemoryStream(Convert.FromHexString(bytes)));
            store.Set(12, id, script.Encode());
        }
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var reference = new Container(new MemoryStream(encoded.ToArray()));
        store.Set(255, 12, reference.Encode());
        return store;
    }
}
