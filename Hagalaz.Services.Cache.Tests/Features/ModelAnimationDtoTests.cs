using System.Net.Http.Json;

using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.Models;
using Hagalaz.Services.Cache.Features.Models.Dtos;
using Hagalaz.TestSupport;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class ModelAnimationDtoTests
{
    [TestMethod]
    public async Task SequenceDto_RetainsFrameIdsDurationsLoopAndTweenFlag()
    {
        var store = new MemoryFileStore();
        Seed(store, 20, 0, 0, [1, 0, 1, 0, 5, 0, 2, 0, 1, 2, 0, 1, 15, 0]);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var result = await client.GetFromJsonAsync<GetAnimationSequenceDto>("/api/v1/cache/types/animations/0/sequence");
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Id);
        CollectionAssert.AreEqual(new[] { 65538 }, result.FrameIds);
        CollectionAssert.AreEqual(new[] { 5 }, result.FrameDurations);
        Assert.AreEqual(1, result.Loop);
        Assert.IsTrue(result.IsTweened);
    }

    [TestMethod]
    public async Task FrameDto_RetainsEveryTransformAndSkeletonField()
    {
        var store = new MemoryFileStore();
        Seed(store, 0, 1, 0, [0, 0, 3, 0, 1, 7, 69, 70, 71]);
        using var skeleton = new Container(new MemoryStream([0, 1, 1, 1, 255, 255, 1, 0]));
        store.Set(1, 3, skeleton.Encode());
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var result = await client.GetFromJsonAsync<GetAnimationFrameDto>("/api/v1/cache/animation-frames/65536");
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Frame.SkeletonId);
        CollectionAssert.AreEqual(new[] { new AnimationTransformDto(0, 5, 6, 7, -1, 0) }, result.Frame.Transforms);
        CollectionAssert.AreEqual(new[] { 1 }, result.Skeleton.Types);
        CollectionAssert.AreEqual(new[] { true }, result.Skeleton.Enabled);
        CollectionAssert.AreEqual(new[] { 65535 }, result.Skeleton.Masks);
        Assert.HasCount(1, result.Skeleton.Groups);
        CollectionAssert.AreEqual(new[] { 0 }, result.Skeleton.Groups[0]);
    }

    [TestMethod]
    public async Task FrameDto_ResolvesSparseFrameIdThroughReferenceTable()
    {
        var store = new MemoryFileStore();
        // Frame member ID 7 is stored at physical position 0.
        Seed(store, 0, 1, 7, [0, 0, 3, 0, 1, 7, 69, 70, 71]);
        using var skeleton = new Container(new MemoryStream([0, 1, 1, 1, 255, 255, 1, 0]));
        store.Set(1, 3, skeleton.Encode());
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var result = await client.GetFromJsonAsync<GetAnimationFrameDto>("/api/v1/cache/animation-frames/65543");
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Frame.SkeletonId);
        CollectionAssert.AreEqual(new[] { new AnimationTransformDto(0, 5, 6, 7, -1, 0) }, result.Frame.Transforms);
    }

    [TestMethod]
    public async Task ClipBatch_ReturnsEachSequenceWithItsDecodedFramesInOneCall()
    {
        var store = new MemoryFileStore();
        // Animation 0 uses frame 65538 (group 1, member 2), which is seeded with its skeleton.
        Seed(store, 20, 0, 0, [1, 0, 1, 0, 5, 0, 2, 0, 1, 2, 0, 1, 15, 0]);
        Seed(store, 0, 1, 2, [0, 0, 3, 0, 1, 7, 69, 70, 71]);
        using var skeleton = new Container(new MemoryStream([0, 1, 1, 1, 255, 255, 1, 0]));
        store.Set(1, 3, skeleton.Encode());
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/animations/clips", new[] { 0, 0 });
        response.EnsureSuccessStatusCode();
        var rows = await response.Content.ReadFromJsonAsync<AnimationClipResultDto[]>();
        Assert.IsNotNull(rows);
        Assert.HasCount(1, rows);
        var clip = rows[0];
        Assert.AreEqual(200, clip.Status);
        CollectionAssert.AreEqual(new[] { 65538 }, clip.Sequence!.FrameIds);
        Assert.HasCount(1, clip.Frames!);
        Assert.AreEqual(65538, clip.Frames![0].FrameId);
        CollectionAssert.AreEqual(new[] { new AnimationTransformDto(0, 5, 6, 7, -1, 0) }, clip.Frames[0].Frame.Frame.Transforms);
        CollectionAssert.AreEqual(new[] { 1 }, clip.Frames[0].Frame.Skeleton.Types);
    }

    [TestMethod]
    public async Task ClipBatch_ReportsAMissingFrameOnItsOwnRow()
    {
        var store = new MemoryFileStore();
        Seed(store, 20, 0, 0, [1, 0, 1, 0, 5, 0, 2, 0, 1, 2, 0, 1, 15, 0]);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/animations/clips", new[] { 0 });
        response.EnsureSuccessStatusCode();
        var rows = await response.Content.ReadFromJsonAsync<AnimationClipResultDto[]>();
        Assert.IsNotNull(rows);
        Assert.AreEqual(404, rows[0].Status);
        Assert.IsNull(rows[0].Frames);
        Assert.IsNotNull(rows[0].Problem);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(33)]
    public async Task ClipBatch_RejectsEmptyOversizedOrNegativeRequests(int count)
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var sized = await client.PostAsJsonAsync("/api/v1/cache/types/animations/clips", Enumerable.Range(0, count).ToArray());
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, sized.StatusCode);
        using var negative = await client.PostAsJsonAsync("/api/v1/cache/types/animations/clips", new[] { -1 });
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, negative.StatusCode);
    }

    private static void Seed(MemoryFileStore store, int index, int group, int child, byte[] data)
    {
        var table = new ReferenceTable(7, 1, 0, 1);
        var entry = new ReferenceTableEntry(0);
        entry.InitializeEntries(1);
        entry.AddEntry(child, new ReferenceTableChildEntry(0));
        table.AddEntry(group, entry);
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var reference = new Container(new MemoryStream(encoded.ToArray()));
        using var container = new Container(new MemoryStream(data));
        store.Set(255, index, reference.Encode());
        store.Set(index, group, container.Encode());
    }
}
