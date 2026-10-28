using System.Net;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Tests.Features;
using Hagalaz.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public sealed class ItemSpriteImageTests
{
    [TestMethod]
    public async Task ItemImageEndpoint_ReturnsGifBytesAndContentType()
    {
        var webRoot = CreateWebRoot();
        var expected = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00, 0xff };
        await File.WriteAllBytesAsync(Path.Combine(webRoot, "images", "items", "42.gif"), expected);

        try
        {
            await using var app = await StartAsync(webRoot);
            using var client = CacheServiceTestHost.CreateClient(app);
            using var response = await client.GetAsync("/api/v1/cache/types/items/42/image");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("image/gif", response.Content.Headers.ContentType?.MediaType);
            var cacheControl = response.Headers.CacheControl;
            Assert.IsNotNull(cacheControl);
            Assert.IsTrue(cacheControl.Public);
            Assert.AreEqual(TimeSpan.FromDays(1), cacheControl.MaxAge);
            CollectionAssert.AreEqual(expected, await response.Content.ReadAsByteArrayAsync());
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [TestMethod]
    public async Task ItemImageEndpoint_ReturnsNotModifiedWhenFileHasNotChanged()
    {
        var webRoot = CreateWebRoot();
        await File.WriteAllBytesAsync(Path.Combine(webRoot, "images", "items", "42.gif"), [0x47, 0x49, 0x46]);

        try
        {
            await using var app = await StartAsync(webRoot);
            using var client = CacheServiceTestHost.CreateClient(app);
            using var response = await client.GetAsync("/api/v1/cache/types/items/42/image");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var lastModified = response.Content.Headers.LastModified;
            Assert.IsNotNull(lastModified);

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cache/types/items/42/image");
            request.Headers.IfModifiedSince = lastModified;
            using var revalidatedResponse = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.NotModified, revalidatedResponse.StatusCode);
            var cacheControl = revalidatedResponse.Headers.CacheControl;
            Assert.IsNotNull(cacheControl);
            Assert.IsTrue(cacheControl.Public);
            Assert.AreEqual(TimeSpan.FromDays(1), cacheControl.MaxAge);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [TestMethod]
    public async Task ItemImageEndpoint_ReturnsNotFoundWhenGifIsMissing()
    {
        var webRoot = CreateWebRoot();

        try
        {
            await using var app = await StartAsync(webRoot);
            using var client = CacheServiceTestHost.CreateClient(app);
            using var response = await client.GetAsync("/api/v1/cache/types/items/42/image");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.IsNull(response.Headers.CacheControl);
            Assert.AreEqual(0, (await response.Content.ReadAsByteArrayAsync()).Length);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    private static string CreateWebRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(path, "images", "items"));
        return path;
    }

    private static Task<WebApplication> StartAsync(string webRoot) =>
        CacheServiceTestHost.StartAsync(new MemoryFileStore(), cacheGroup => cacheGroup.MapTypeEndpoints(), webRootPath: webRoot);
}
