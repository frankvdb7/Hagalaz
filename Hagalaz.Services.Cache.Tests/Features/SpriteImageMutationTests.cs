using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public sealed class SpriteImageMutationTests
{
    [TestMethod]
    public async Task ReplaceSpriteImage_PngPersistsAndCanBeRead()
    {
        var color = new Rgba32(240, 20, 40, 255);
        await AssertReplacementRoundTripsAsync(CreatePng(color), [color]);
    }

    [TestMethod]
    public async Task ReplaceSpriteImage_AnimatedGifPreservesEveryFrame()
    {
        var firstFrameColor = new Rgba32(255, 0, 0, 255);
        var secondFrameColor = new Rgba32(0, 0, 255, 255);
        await AssertReplacementRoundTripsAsync(CreateAnimatedGif(firstFrameColor, secondFrameColor), [firstFrameColor, secondFrameColor]);
    }

    private static async Task AssertReplacementRoundTripsAsync(byte[] sourceBytes, Rgba32[] expectedFrameColors)
    {
        const int spriteId = 0;
        var store = new MemoryFileStore();
        var referenceTableCodec = new ReferenceTableCodec();
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var archiveEntry = new ReferenceTableEntry(0);
        archiveEntry.InitializeEntries(1);
        archiveEntry.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(0, archiveEntry);

        using (var encodedTable = referenceTableCodec.Encode(table))
        {
            store.Set(255, 8, EncodeContainer(encodedTable.ToArray()));
        }

        store.Set(8, 0, EncodeContainer([0]));

        await using var app = await CacheServiceTestHost.StartAsync(store, cacheGroup => cacheGroup.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var content = new ByteArrayContent(sourceBytes);
        using var mutationResponse = await client.PostAsync($"/api/v1/cache/types/sprites/{spriteId}/image", content);
        Assert.AreEqual(HttpStatusCode.OK, mutationResponse.StatusCode, await mutationResponse.Content.ReadAsStringAsync());

        var mutation = await mutationResponse.Content.ReadFromJsonAsync<TypeMutationEndpoints.SpriteMutationResponse>();
        Assert.IsNotNull(mutation);
        Assert.AreEqual(2, mutation.Width);
        Assert.AreEqual(1, mutation.Height);
        Assert.AreEqual(expectedFrameColors.Length, mutation.FrameCount);

        var sprite = await client.GetFromJsonAsync<TypeEndpoints.SpriteTypeResponse>($"/api/v1/cache/types/sprites/{spriteId}");
        Assert.IsNotNull(sprite);
        Assert.AreEqual(2, sprite.Width);
        Assert.AreEqual(1, sprite.Height);
        Assert.AreEqual(expectedFrameColors.Length, sprite.FrameCount);

        for (var frameIndex = 0; frameIndex < expectedFrameColors.Length; frameIndex++)
        {
            using var imageResponse = await client.GetAsync($"/api/v1/cache/types/sprites/{spriteId}/image?frame={frameIndex}");
            Assert.AreEqual(HttpStatusCode.OK, imageResponse.StatusCode, await imageResponse.Content.ReadAsStringAsync());

            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
            using var image = Image.Load<Rgba32>(imageBytes);
            Assert.AreEqual(2, image.Width);
            Assert.AreEqual(1, image.Height);
            Assert.AreEqual(expectedFrameColors[frameIndex], image[0, 0]);
            Assert.AreEqual(expectedFrameColors[frameIndex], image[1, 0]);
        }
    }

    private static byte[] CreatePng(Rgba32 color)
    {
        using var image = new Image<Rgba32>(2, 1);
        Fill(image.Frames.RootFrame, color);
        using var output = new MemoryStream();
        image.SaveAsPng(output);
        return output.ToArray();
    }

    private static byte[] CreateAnimatedGif(Rgba32 firstFrameColor, Rgba32 secondFrameColor)
    {
        using var image = new Image<Rgba32>(2, 1);
        Fill(image.Frames.RootFrame, firstFrameColor);

        using var secondFrame = new Image<Rgba32>(2, 1);
        Fill(secondFrame.Frames.RootFrame, secondFrameColor);
        image.Frames.AddFrame(secondFrame.Frames.RootFrame);

        using var output = new MemoryStream();
        image.SaveAsGif(output);
        return output.ToArray();
    }

    private static void Fill(ImageFrame<Rgba32> frame, Rgba32 color)
    {
        for (var x = 0; x < frame.Width; x++)
        {
            frame[x, 0] = color;
        }
    }

    private static byte[] EncodeContainer(byte[] data)
    {
        using var container = new Container(new MemoryStream(data));
        return container.Encode();
    }
}
