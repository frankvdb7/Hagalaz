using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

/// <summary>Round-trips every stored entry of the codec-backed definition types in the local game cache, when it is present.</summary>
[TestClass]
public class RealCacheCodecRoundTripTests
{
    private static readonly string CachePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Cache"));

    [TestMethod]
    public void ParticleEmitters_RoundTrip() => RoundTripMembers<IParticleEmitterType>(27, 0);

    [TestMethod]
    public void ParticleEffectors_RoundTrip() => RoundTripMembers<IParticleEffectorType>(27, 1);

    [TestMethod]
    public void Billboards_RoundTrip() => RoundTripMembers<IBillboardType>(29, 0);

    [TestMethod]
    public void RenderAnimations_RoundTrip() => RoundTripMembers<IRenderAnimationType>(2, 32);

    [TestMethod]
    public void MaterialTable_RoundTripsByteForByte()
    {
        using var provider = OpenCache();
        var cache = provider.GetRequiredService<ICacheAPI>();
        var codec = provider.GetRequiredService<ITypeCodec<IMaterialTableType>>();
        using var stored = cache.Read(26, 0, 0);
        var bytes = stored.ToArray();

        using var encoded = codec.Encode(codec.Decode(0, new MemoryStream(bytes)));

        CollectionAssert.AreEqual(bytes, encoded.ToArray());
    }

    [TestMethod]
    public void Models_RoundTripByteForByte()
    {
        using var provider = OpenCache();
        var cache = provider.GetRequiredService<ICacheAPI>();
        var codec = provider.GetRequiredService<ITypeCodec<IModelType>>();
        var mismatches = new List<int>();
        foreach (var (id, _) in cache.ReadReferenceTable(7).Entries)
        {
            using var container = cache.ReadContainer(7, id);
            var bytes = container.Data.ToArray();
            using var encoded = codec.Encode(codec.Decode(id, new MemoryStream(bytes)));
            if (!encoded.ToArray().AsSpan().SequenceEqual(bytes)) mismatches.Add(id);
        }
        Assert.AreEqual(0, mismatches.Count, "Models not byte-identical: " + string.Join(", ", mismatches.Take(20)));
    }

    internal static ServiceProvider OpenCache()
    {
        if (!Directory.Exists(CachePath)) Assert.Inconclusive($"No local game cache at {CachePath}.");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameCache(options => options.Path = CachePath);
        return services.BuildServiceProvider();
    }

    internal static IEnumerable<(int Id, byte[] Bytes)> Members(ICacheAPI cache, int index, int archive)
    {
        var entry = cache.ReadReferenceTable(index).GetEntry(archive)!;
        foreach (var (memberId, member) in entry.Entries)
        {
            using var data = cache.Read(index, archive, member.Index);
            yield return (memberId, data.ToArray());
        }
    }

    private static void RoundTripMembers<TType>(int index, int archive) where TType : IType
    {
        using var provider = OpenCache();
        var cache = provider.GetRequiredService<ICacheAPI>();
        var codec = provider.GetRequiredService<ITypeCodec<TType>>();
        var count = 0;
        foreach (var (id, bytes) in Members(cache, index, archive))
        {
            using var first = codec.Encode(codec.Decode(id, new MemoryStream(bytes)));
            using var second = codec.Encode(codec.Decode(id, new MemoryStream(first.ToArray())));
            CollectionAssert.AreEqual(first.ToArray(), second.ToArray(), $"{typeof(TType).Name} {id} is not stable.");
            count++;
        }
        Assert.IsTrue(count > 0, $"No {typeof(TType).Name} entries were read.");
    }
}
