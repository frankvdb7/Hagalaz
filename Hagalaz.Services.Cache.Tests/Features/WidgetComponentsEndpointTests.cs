using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Features.Widgets;
using Hagalaz.Services.Cache.Security;
using Hagalaz.TestSupport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public sealed class WidgetComponentsEndpointTests
{
    // Index 3 members from the pinned revision-742 cache.
    private const string Camera = "/wAAAAAFAAUAgAAQAAACAAAaAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAAARcAAAAAAQAAB7MA";
    private const string Clan = "/wAAAAAAAAAAEAAAAQEAAADxAAAAAAAAAAAAAAAAAAAAAAAKAAAAEZEAAAAOfwEAAAAAAJAAAAAACAAESADoAARIAPMABEgA9QCAAAADAARIAVgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string Sprite = "/wUAAAAKAAMAEAAQAAACAAAFAAAAAz8AAAAAAAAAAAAAAP////8AAAIAAAFDbG9zZQAAAAAAAAADAAAAACwAgAAAAwAAAANAAwAAAAAsAIAAAAMAAAADPwAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string Empty = "/wAAAAAFAAYAEAAQAAAAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private const string Route = "/api/v1/cache/widgets";

    [TestMethod]
    public async Task Components_DecodesPinnedCallbackAndPreservesPreviewBytes()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapWidgetComponentEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var response = await client.GetFromJsonAsync<JsonElement>($"{Route}/977/components");
        var entry = response.GetProperty("entries")[0];
        Assert.AreEqual(0, entry.GetProperty("subFileId").GetInt32());
        Assert.AreEqual(Camera, entry.GetProperty("dataBase64").GetString());
        var callback = entry.GetProperty("callbacks")[0];
        Assert.AreEqual("CS2 integer changed", callback.GetProperty("event").GetString());
        Assert.AreEqual(279, callback.GetProperty("scriptId").GetInt32());
        Assert.AreEqual(1971, callback.GetProperty("triggers")[0].GetInt32());
    }

    [TestMethod]
    public async Task Components_PreservesArgumentTypesAndCallbackOrder()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapWidgetComponentEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var clan = await client.GetFromJsonAsync<JsonElement>($"{Route}/1096/components");
        var args = clan.GetProperty("entries")[0].GetProperty("callbacks")[0].GetProperty("args");
        Assert.AreEqual("int", args[0].GetProperty("type").GetString());
        Assert.AreEqual(3711, args[0].GetProperty("value").GetInt32());
        Assert.AreEqual("string", args[1].GetProperty("type").GetString());
        Assert.AreEqual("", args[1].GetProperty("value").GetString());

        var sprite = await client.GetFromJsonAsync<JsonElement>($"{Route}/742/components");
        var callbacks = sprite.GetProperty("entries")[0].GetProperty("callbacks");
        CollectionAssert.AreEqual(new[] { "Mouse entered", "Mouse exited" },
            callbacks.EnumerateArray().Select(callback => callback.GetProperty("event").GetString()).ToArray());
        Assert.AreEqual(831, callbacks[1].GetProperty("args")[1].GetProperty("value").GetInt32());
    }

    [TestMethod]
    public async Task Components_DistinguishesEmptyTruncatedAndMissingMetadata()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapWidgetComponentEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var empty = await client.GetFromJsonAsync<JsonElement>($"{Route}/182/components");
        Assert.AreEqual(0, empty.GetProperty("entries")[0].GetProperty("callbacks").GetArrayLength());
        var truncated = await client.GetFromJsonAsync<JsonElement>($"{Route}/1/components");
        var entry = truncated.GetProperty("entries")[0];
        Assert.AreEqual(JsonValueKind.Null, entry.GetProperty("callbacks").ValueKind);
        Assert.AreEqual(Convert.ToBase64String(Convert.FromBase64String(Camera)[..^2]), entry.GetProperty("dataBase64").GetString());
        using var missing = await client.GetAsync($"{Route}/2/components");
        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [TestMethod]
    public async Task Program_LoadsCallbackRootsAndCalleesOnceAcrossCycles()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapWidgetPrograms());
        using var client = CacheServiceTestHost.CreateClient(app);
        var bundle = await client.GetFromJsonAsync<JsonElement>($"{Route}/977/program");
        Assert.AreEqual(1, bundle.GetProperty("version").GetInt32());
        Assert.AreEqual(279, bundle.GetProperty("bindings")[0].GetProperty("callbacks")[0].GetProperty("scriptId").GetInt32());
        var programs = bundle.GetProperty("programs");
        CollectionAssert.AreEqual(new[] { 279, 280, 281 }, programs.EnumerateArray().Select(p => p.GetProperty("id").GetInt32()).ToArray());
        Assert.AreEqual("call", programs[0].GetProperty("instructions")[0].GetProperty("operation").GetString());
        Assert.AreEqual("280", programs[0].GetProperty("instructions")[0].GetProperty("operand").GetString());
        Assert.AreEqual("getVarbit", programs[1].GetProperty("instructions")[0].GetProperty("operation").GetString());
        var varbit = bundle.GetProperty("varbits").GetProperty("7");
        Assert.AreEqual(2, varbit.GetProperty("configId").GetInt32());
        Assert.AreEqual(4, varbit.GetProperty("start").GetInt32());
        Assert.AreEqual(6, varbit.GetProperty("end").GetInt32());
        Assert.AreEqual(999, bundle.GetProperty("missing")[0].GetInt32());
    }

    [TestMethod]
    public async Task Components_InheritsCacheAuthorization()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapCacheEndpoints());
        var route = app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText!.EndsWith("/widgets/{widgetId:int}/components", StringComparison.Ordinal));
        Assert.IsTrue(route.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(policy => policy.Policy == CacheAuthorization.PolicyName));
    }

    private static MemoryFileStore Store()
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 1, 0, 5);
        foreach (var (widget, component, bytes) in new[]
        {
            (1, 30, Convert.FromBase64String(Camera)[..^2]), (182, 0, Convert.FromBase64String(Empty)),
            (742, 18, Convert.FromBase64String(Sprite)), (977, 30, Convert.FromBase64String(Camera)),
            (1096, 244, Convert.FromBase64String(Clan))
        })
        {
            var entry = new ReferenceTableEntry(widget);
            entry.InitializeEntries(1);
            entry.AddEntry(component, new ReferenceTableChildEntry(0));
            table.AddEntry(widget, entry);
            using var member = new Container(new MemoryStream(bytes));
            store.Set(3, widget, member.Encode());
        }
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var reference = new Container(new MemoryStream(encoded.ToArray()));
        store.Set(255, 3, reference.Encode());
        var scripts = new ReferenceTable(7, 1, 0, 3);
        foreach (var (id, count, code) in new[] {
            (279, 4, "0003000001180003000001180003000003E7000200"),
            (280, 3, "021D00000007000300000119000200"),
            (281, 2, "000300000117000200")
        })
        {
            var entry = new ReferenceTableEntry(id);
            entry.InitializeEntries(1);
            entry.AddEntry(0, new ReferenceTableChildEntry(0));
            scripts.AddEntry(id, entry);
            using var script = new Container(new MemoryStream(Convert.FromHexString(
                $"00{code}{count:X8}000000000000000000000000000001")));
            store.Set(12, id, script.Encode());
        }
        using var scriptTable = new ReferenceTableCodec().Encode(scripts);
        using var scriptReference = new Container(new MemoryStream(scriptTable.ToArray()));
        store.Set(255, 12, scriptReference.Encode());
        var varbits = new ReferenceTable(7, 1, 0, 1);
        var bitArchive = new ReferenceTableEntry(0);
        bitArchive.InitializeEntries(1);
        bitArchive.AddEntry(7, new ReferenceTableChildEntry(0));
        varbits.AddEntry(0, bitArchive);
        using var bit = new Container(new MemoryStream(Convert.FromHexString("010002040600")));
        store.Set(22, 0, bit.Encode());
        using var bitTable = new ReferenceTableCodec().Encode(varbits);
        using var bitReference = new Container(new MemoryStream(bitTable.ToArray()));
        store.Set(255, 22, bitReference.Encode());
        return store;
    }
}
