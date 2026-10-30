using System.Net;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public sealed class ModelAssetEndpointsTests
{
    [TestMethod]
    [DataRow("models/-1", HttpStatusCode.BadRequest)]
    [DataRow("models/999", HttpStatusCode.NotFound)]
    [DataRow("textures/-1/image", HttpStatusCode.BadRequest)]
    [DataRow("textures/999/image", HttpStatusCode.NotFound)]
    [DataRow("types/items/-1/model", HttpStatusCode.BadRequest)]
    [DataRow("types/npcs/-1/model", HttpStatusCode.BadRequest)]
    [DataRow("types/objects/-1/model", HttpStatusCode.BadRequest)]
    public async Task AssetRequest_InvalidOrMissingIdReturnsExpectedStatus(string path, HttpStatusCode expected)
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group =>
        {
            group.MapModelEndpoints();
            group.MapTextureEndpoints();
        });
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.GetAsync("/api/v1/cache/" + path);

        Assert.AreEqual(expected, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task ModelRequest_ExtendedSkinFormatReturnsExplicitUnsupportedProblem()
    {
        var data = new byte[23];
        data[5] = 16;
        data[^1] = 255; data[^2] = 255;
        using var container = new Container(new MemoryStream(data));
        var store = new MemoryFileStore();
        store.Set(7, 0, container.Encode());
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.GetAsync("/api/v1/cache/models/0");

        Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "Extended model skin tables");
    }
}
