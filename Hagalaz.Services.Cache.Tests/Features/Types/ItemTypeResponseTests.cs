using System.Text.Json;
using Hagalaz.Cache;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Services.Cache.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public class ItemTypeResponseTests
{
    [TestMethod]
    public async Task ItemEndpoint_MapsDecodedReferencesAndPreservesActionSlots()
    {
        // Real revision-742 opcodes, including a valid zero reference and sparse actions.
        using var bytes = new MemoryStream([
            1, 0, 42,
            97, 0, 0, 98, 0, 8,
            121, 0, 9, 122, 0, 10,
            139, 0, 11, 140, 0, 12,
            35, (byte)'W', (byte)'e', (byte)'a', (byte)'r', 0,
            33, (byte)'T', (byte)'e', (byte)'s', (byte)'t', 0,
            0
        ]);
        var item = new ItemTypeCodec().Decode(7, bytes);

        using var response = await ReadItem(item);
        var json = response.RootElement;

        Assert.AreEqual(7, json.GetProperty("id").GetInt32());
        Assert.AreEqual(42, json.GetProperty("interfaceModelId").GetInt32());
        Assert.AreEqual(0, json.GetProperty("noteId").GetInt32());
        Assert.AreEqual(8, json.GetProperty("noteTemplateId").GetInt32());
        Assert.AreEqual(9, json.GetProperty("lendId").GetInt32());
        Assert.AreEqual(10, json.GetProperty("lendTemplateId").GetInt32());
        Assert.AreEqual(11, json.GetProperty("boughtItemId").GetInt32());
        Assert.AreEqual(12, json.GetProperty("boughtTemplateId").GetInt32());
        CollectionAssert.AreEqual(new string?[] { "Wear", null, null, null, "drop" },
            json.GetProperty("inventoryOptions").EnumerateArray().Select(value => value.GetString()).ToArray());
        CollectionAssert.AreEqual(new string?[] { null, null, "take", "Test", null },
            json.GetProperty("groundOptions").EnumerateArray().Select(value => value.GetString()).ToArray());
        Assert.IsFalse(json.TryGetProperty("tradePrice", out _));
    }

    [TestMethod]
    public async Task ItemEndpoint_PreservesAbsentRelationshipSentinels()
    {
        using var bytes = new MemoryStream([0]);
        var item = new ItemTypeCodec().Decode(0, bytes);

        using var response = await ReadItem(item);

        foreach (var property in new[] { "noteId", "noteTemplateId", "lendId", "lendTemplateId", "boughtItemId", "boughtTemplateId" })
            Assert.AreEqual(-1, response.RootElement.GetProperty(property).GetInt32(), property);
        Assert.IsFalse(response.RootElement.GetProperty("membersOnly").GetBoolean());
    }

    private static async Task<JsonDocument> ReadItem(IItemType item)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<CacheOptions>(options => options.Path = "./Cache");
        builder.Services.AddGameCache();
        builder.Services.AddSingleton<ITypeProvider<IItemType>>(new ItemProvider(item));
        await using var app = builder.Build();
        app.MapTypeEndpoints();
        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(endpoint => endpoint.RoutePattern.RawText == "/types/items/{id:int}");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.RouteValues["id"] = item.Id.ToString();
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    private sealed class ItemProvider(IItemType item) : ITypeProvider<IItemType>
    {
        public int ArchiveSize => item.Id + 1;
        public IItemType Get(int typeId) => item;
        public IItemType[] GetAll() => [item];
        public IItemType[] GetRange(int startTypeId, int endTypeId) => [item];
    }
}
