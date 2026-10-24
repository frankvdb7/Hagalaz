using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Providers;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class TypeProviderSnapshotTests
{
    [Fact]
    public void GenericGet_UsesPhysicalMemberAndKeepsFallbackCustomDelegationAndHookFrequency()
    {
        var cache = new Mock<ICacheAPI>();
        var table = ReferenceTableWithChild(1, 2, 11);
        cache.Setup(current => current.ReadReferenceTable(2)).Returns(table.Object);
        var firstStream = new MemoryStreamSpy();
        var secondStream = new MemoryStreamSpy();
        cache.SetupSequence(current => current.Read(2, 1, 11)).Returns(firstStream).Returns(secondStream);

        var first = new Mock<IQuestType>().Object;
        var second = new Mock<IQuestType>().Object;
        var codec = new Mock<ITypeCodec<IQuestType>>();
        codec.SetupSequence(current => current.Decode(10, It.IsAny<MemoryStream>())).Returns(first).Returns(second);
        var factory = new Mock<ITypeFactory<IQuestType>>();
        var hook = new Mock<ITypeEventHook<IQuestType>>();
        var provider = new TypeProvider<IQuestType, QuestTypeData>(cache.Object, factory.Object, codec.Object, hook.Object);

        Assert.Same(first, provider.Get(10));
        Assert.Same(second, provider.Get(10));
        Assert.True(firstStream.IsDisposed);
        Assert.True(secondStream.IsDisposed);
        cache.Verify(current => current.Read(2, 1, 11), Times.Exactly(2));
        hook.Verify(current => current.AfterDecode(provider, It.Is<IQuestType[]>(types => types.Length == 1)), Times.Exactly(2));

        var fallback = new Mock<IQuestType>().Object;
        table = ReferenceTableWithChild(1, 2, null);
        cache.Setup(current => current.ReadReferenceTable(2)).Returns(table.Object);
        factory.Setup(current => current.CreateType(10)).Returns(fallback);
        Assert.Same(fallback, provider.Get(10));
        cache.Verify(current => current.Read(2, 1, It.IsAny<int>()), Times.Exactly(2));
        hook.Verify(current => current.AfterDecode(provider, It.IsAny<IQuestType[]>()), Times.Exactly(2));

        var customType = new Mock<IQuestType>().Object;
        var customProvider = new Mock<ITypeProvider<IQuestType>>();
        customProvider.Setup(current => current.Get(10)).Returns(customType);
        var delegated = new TypeProvider<IQuestType, QuestTypeData>(cache.Object, factory.Object, codec.Object, hook.Object, customProvider.Object);
        Assert.Same(customType, delegated.Get(10));
        cache.Verify(current => current.ReadReferenceTable(2), Times.Exactly(3));
        customProvider.Verify(current => current.Get(10), Times.Once);
    }

    [Fact]
    public void GenericGetRange_KeepsArchiveBatchReadAndSingleHookCall()
    {
        var cache = new Mock<ICacheAPI>();
        var table = ReferenceTableWithChild(1, 0, 3);
        table.Setup(current => current.GetEntry(1, 1)).Returns(CreateChild(5));
        cache.Setup(current => current.ReadReferenceTable(2)).Returns(table.Object);
        var archive = new Mock<IArchive>();
        using var firstMember = new MemoryStream([1]);
        using var secondMember = new MemoryStream([2]);
        archive.Setup(current => current.GetEntry(3)).Returns(firstMember);
        archive.Setup(current => current.GetEntry(5)).Returns(secondMember);
        cache.Setup(current => current.ReadArchive(2, 1)).Returns(archive.Object);
        var first = new Mock<IQuestType>().Object;
        var second = new Mock<IQuestType>().Object;
        var codec = new Mock<ITypeCodec<IQuestType>>();
        codec.Setup(current => current.Decode(8, It.IsAny<MemoryStream>())).Returns(first);
        codec.Setup(current => current.Decode(9, It.IsAny<MemoryStream>())).Returns(second);
        var hook = new Mock<ITypeEventHook<IQuestType>>();
        var provider = new TypeProvider<IQuestType, QuestTypeData>(
            cache.Object,
            new Mock<ITypeFactory<IQuestType>>().Object,
            codec.Object,
            hook.Object);

        var result = provider.GetRange(8, 10);

        Assert.Equal(new[] { first, second }, result);
        archive.Verify(current => current.GetEntry(3), Times.Once);
        archive.Verify(current => current.GetEntry(5), Times.Once);
        cache.Verify(current => current.ReadArchive(2, 1), Times.Once);
        cache.Verify(current => current.Read(2, 1, It.IsAny<int>()), Times.Never);
        hook.Verify(current => current.AfterDecode(provider, It.Is<IQuestType[]>(types => types.Length == 2)), Times.Once);
    }

    [Fact]
    public void ObjectGet_UsesPhysicalMemberAndProducesFreshDefinitions()
    {
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(16)).Returns(ReferenceTableWithChild(1, 3, 7).Object);
        var streams = new[] { new MemoryStreamSpy(), new MemoryStreamSpy() };
        cache.SetupSequence(current => current.Read(16, 1, 7)).Returns(streams[0]).Returns(streams[1]);
        var first = new ObjectType(259) { Name = "cache" };
        var second = new ObjectType(259) { Name = "cache" };
        var decoded = new Queue<IObjectType>([first, second]);
        var codec = new Mock<IObjectTypeCodec>();
        codec.Setup(current => current.Decode(259, It.IsAny<MemoryStream>())).Returns(() => decoded.Dequeue());
        var hook = new Mock<ITypeEventHook<IObjectType>>();
        var provider = new ObjectTypeProvider(cache.Object, codec.Object, new Mock<ITypeFactory<IObjectType>>().Object, hook.Object);

        var actualFirst = provider.Get(259);
        var actualSecond = provider.Get(259);
        ((ObjectType)actualFirst).Name = "database override";

        Assert.NotSame(actualFirst, actualSecond);
        Assert.Equal("cache", actualSecond.Name);
        Assert.All(streams, stream => Assert.True(stream.IsDisposed));
        cache.Verify(current => current.Read(16, 1, 7), Times.Exactly(2));
        hook.Verify(current => current.AfterDecode(provider, It.Is<IObjectType[]>(types => types.Length == 1)), Times.Exactly(2));
    }

    [Fact]
    public void ItemGet_UsesPhysicalMember()
    {
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(19)).Returns(ReferenceTableWithChild(1, 3, 9).Object);
        var stream = new MemoryStreamSpy();
        cache.Setup(current => current.Read(19, 1, 9)).Returns(stream);
        var decoded = new Mock<IItemType>().Object;
        var codec = new Mock<IItemTypeCodec>();
        codec.Setup(current => current.Decode(259, stream)).Returns(decoded);
        var hook = new Mock<ITypeEventHook<IItemType>>();
        var provider = new ItemTypeProvider(cache.Object, codec.Object, new Mock<ITypeFactory<IItemType>>().Object, hook.Object);

        Assert.Same(decoded, provider.Get(259));

        Assert.True(stream.IsDisposed);
        cache.Verify(current => current.Read(19, 1, 9), Times.Once);
        hook.Verify(current => current.AfterDecode(provider, It.Is<IItemType[]>(types => types.Length == 1)), Times.Once);
    }

    [Fact]
    public void NpcGet_UsesPhysicalMember()
    {
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(18)).Returns(ReferenceTableWithChild(1, 5, 6).Object);
        var stream = new MemoryStreamSpy();
        cache.Setup(current => current.Read(18, 1, 6)).Returns(stream);
        var decoded = new Mock<INpcType>().Object;
        var codec = new Mock<INpcTypeCodec>();
        codec.Setup(current => current.Decode(133, stream)).Returns(decoded);
        var hook = new Mock<ITypeEventHook<INpcType>>();
        var provider = new NpcTypeProvider(cache.Object, codec.Object, new Mock<ITypeFactory<INpcType>>().Object, hook.Object);

        Assert.Same(decoded, provider.Get(133));

        Assert.True(stream.IsDisposed);
        cache.Verify(current => current.Read(18, 1, 6), Times.Once);
        hook.Verify(current => current.AfterDecode(provider, It.Is<INpcType[]>(types => types.Length == 1)), Times.Once);
    }

    [Fact]
    public void SpriteGet_UsesPhysicalMember()
    {
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(8)).Returns(ReferenceTableWithChild(5, 0, 13).Object);
        var stream = new MemoryStreamSpy();
        cache.Setup(current => current.Read(8, 5, 13)).Returns(stream);
        var decoded = new Mock<ISpriteType>().Object;
        var codec = new Mock<ISpriteTypeCodec>();
        codec.Setup(current => current.Decode(5, stream)).Returns(decoded);
        var hook = new Mock<ITypeEventHook<ISpriteType>>();
        var provider = new SpriteTypeProvider(cache.Object, codec.Object, new Mock<ITypeFactory<ISpriteType>>().Object, hook.Object);

        Assert.Same(decoded, provider.Get(5));

        Assert.True(stream.IsDisposed);
        cache.Verify(current => current.Read(8, 5, 13), Times.Once);
        hook.Verify(current => current.AfterDecode(provider, It.Is<ISpriteType[]>(types => types.Length == 1)), Times.Once);
    }

    private static Mock<IReferenceTable> ReferenceTableWithChild(int archiveId, int childId, int? physicalIndex)
    {
        var table = new Mock<IReferenceTable>();
        if (physicalIndex is not null)
        {
            table.Setup(current => current.GetEntry(archiveId, childId)).Returns(CreateChild(physicalIndex.Value));
        }
        else
        {
            table.Setup(current => current.GetEntry(archiveId, childId)).Returns(() => null!);
        }

        return table;
    }

    private static IReferenceTableChildEntry CreateChild(int physicalIndex)
    {
        var child = new Mock<IReferenceTableChildEntry>();
        child.SetupGet(current => current.Index).Returns(physicalIndex);
        return child.Object;
    }
}
