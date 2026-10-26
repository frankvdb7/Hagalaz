using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Providers;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class NpcTypeProviderSparseRangeTests
{
    [Fact]
    public void GetRange_UsesFactoryForMissingArchiveAndMatchesIndividualGets()
    {
        var table = CreateReferenceTableWithArchiveGap();
        Assert.NotNull(table.GetEntry(0));
        Assert.Null(table.GetEntry(1));
        Assert.NotNull(table.GetEntry(2));

        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(18)).Returns(table);
        var firstArchive = CreateArchive();
        var lastArchive = CreateArchive();
        cache.Setup(current => current.ReadArchive(18, 0)).Returns(firstArchive);
        cache.Setup(current => current.ReadArchive(18, 2)).Returns(lastArchive);
        cache.Setup(current => current.Read(18, 0, 0)).Returns(() => new MemoryStream());
        cache.Setup(current => current.Read(18, 2, 0)).Returns(() => new MemoryStream());

        var firstType = new Mock<INpcType>().Object;
        var lastType = new Mock<INpcType>().Object;
        var codec = new Mock<INpcTypeCodec>();
        codec.Setup(current => current.Decode(0, It.IsAny<MemoryStream>())).Returns(firstType);
        codec.Setup(current => current.Decode(256, It.IsAny<MemoryStream>())).Returns(lastType);
        var fallbacks = new Dictionary<int, INpcType>();
        var factory = new Mock<ITypeFactory<INpcType>>();
        factory.Setup(current => current.CreateType(It.IsAny<int>())).Returns((int typeId) =>
        {
            if (!fallbacks.TryGetValue(typeId, out var fallback))
            {
                fallback = new Mock<INpcType>().Object;
                fallbacks.Add(typeId, fallback);
            }

            return fallback;
        });
        var hook = new Mock<ITypeEventHook<INpcType>>();
        var provider = new NpcTypeProvider(cache.Object, codec.Object, factory.Object, hook.Object);

        var range = provider.GetRange(0, 257);

        Assert.Equal(257, range.Length);
        cache.Verify(current => current.ReadArchive(18, 0), Times.Once);
        cache.Verify(current => current.ReadArchive(18, 1), Times.Never);
        cache.Verify(current => current.ReadArchive(18, 2), Times.Once);
        hook.Verify(current => current.AfterDecode(provider, It.Is<INpcType[]>(types => types.Length == 257)), Times.Once);

        for (var typeId = 0; typeId < range.Length; typeId++)
        {
            Assert.Same(provider.Get(typeId), range[typeId]);
        }
    }

    private static ReferenceTable CreateReferenceTableWithArchiveGap()
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 2);
        foreach (var archiveId in new[] { 0, 2 })
        {
            var entry = new ReferenceTableEntry(archiveId);
            entry.InitializeEntries(1);
            entry.AddEntry(0, new ReferenceTableChildEntry(0));
            table.AddEntry(archiveId, entry);
        }

        return table;
    }

    private static Archive CreateArchive()
    {
        var archive = new Archive(1);
        archive.Entries![0] = new MemoryStream();
        return archive;
    }
}
