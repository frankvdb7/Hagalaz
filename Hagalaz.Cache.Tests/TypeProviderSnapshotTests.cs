using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Hooks;
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
        var table = ReferenceTableWithChild(35, 10, 11);
        cache.Setup(current => current.ReadReferenceTable(2)).Returns(table.Object);
        var firstStream = new MemoryStreamSpy();
        var secondStream = new MemoryStreamSpy();
        cache.SetupSequence(current => current.Read(2, 35, 11)).Returns(firstStream).Returns(secondStream);

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
        cache.Verify(current => current.Read(2, 35, 11), Times.Exactly(2));
        hook.Verify(current => current.AfterDecode(provider, It.Is<IQuestType[]>(types => types.Length == 1)), Times.Exactly(2));

        var fallback = new Mock<IQuestType>().Object;
        table = ReferenceTableWithChild(35, 10, null);
        cache.Setup(current => current.ReadReferenceTable(2)).Returns(table.Object);
        factory.Setup(current => current.CreateType(10)).Returns(fallback);
        Assert.Same(fallback, provider.Get(10));
        cache.Verify(current => current.Read(2, 35, It.IsAny<int>()), Times.Exactly(2));
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
    public void GenericGetAndRange_ThrowForAbsentParentAndGetFallsBackForAbsentChild()
    {
        const int typeId = 10;
        var typeData = new QuestTypeData();
        var archiveId = typeData.GetArchiveId(typeId);
        var childId = typeData.GetArchiveEntryId(typeId);
        var absentParent = ReferenceTableWithoutArchive(archiveId);
        var absentChild = ReferenceTableWithArchiveWithoutChild(archiveId, childId);
        Assert.Null(absentParent.GetEntry(archiveId));
        Assert.NotNull(absentChild.GetEntry(archiveId));
        Assert.Null(absentChild.GetEntry(archiveId, childId));

        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(2)).Returns(absentParent);
        cache.Setup(current => current.ReadArchive(2, archiveId)).Throws<FileNotFoundException>();
        var fallback = new Mock<IQuestType>().Object;
        var factory = new Mock<ITypeFactory<IQuestType>>();
        factory.Setup(current => current.CreateType(typeId)).Returns(fallback);
        var provider = new TypeProvider<IQuestType, QuestTypeData>(
            cache.Object,
            factory.Object,
            new Mock<ITypeCodec<IQuestType>>().Object);

        Assert.Throws<FileNotFoundException>(() => provider.Get(typeId));
        Assert.Throws<FileNotFoundException>(() => provider.GetRange(typeId, typeId + 1));
        cache.Verify(current => current.ReadArchive(2, archiveId), Times.Once);
        cache.Verify(current => current.Read(2, archiveId, It.IsAny<int>()), Times.Never);
        factory.Verify(current => current.CreateType(typeId), Times.Never);

        cache.Setup(current => current.ReadReferenceTable(2)).Returns(absentChild);
        Assert.Same(fallback, provider.Get(typeId));

        cache.Verify(current => current.Read(2, archiveId, It.IsAny<int>()), Times.Never);
        factory.Verify(current => current.CreateType(typeId), Times.Once);
    }

    [Fact]
    public void SpriteGet_RequiresParentArchiveAndFallsBackForAbsentChild()
    {
        const int spriteId = 5;
        const int childId = 0;
        var cache = new Mock<ICacheAPI>();
        cache.SetupSequence(current => current.ReadReferenceTable(8))
            .Returns(ReferenceTableWithoutArchive(spriteId))
            .Returns(ReferenceTableWithArchiveWithoutChild(spriteId, childId));
        var fallback = new Mock<ISpriteType>().Object;
        var factory = new Mock<ITypeFactory<ISpriteType>>();
        factory.Setup(current => current.CreateType(spriteId)).Returns(fallback);
        var provider = new SpriteTypeProvider(
            cache.Object,
            new Mock<ISpriteTypeCodec>().Object,
            factory.Object);

        Assert.Throws<FileNotFoundException>(() => provider.Get(spriteId));
        Assert.Same(fallback, provider.Get(spriteId));

        cache.Verify(current => current.Read(8, spriteId, It.IsAny<int>()), Times.Never);
        factory.Verify(current => current.CreateType(spriteId), Times.Once);
    }

    [Fact]
    public void ItemGet_PreservesFactoryFallbackWhenParentArchiveIsAbsent()
    {
        const int itemId = 259;
        var typeData = new ItemTypeData();
        var archiveId = typeData.GetArchiveId(itemId);
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(typeData.IndexId)).Returns(ReferenceTableWithoutArchive(archiveId));
        var fallback = new Mock<IItemType>().Object;
        var factory = new Mock<ITypeFactory<IItemType>>();
        factory.Setup(current => current.CreateType(itemId)).Returns(fallback);
        var provider = new ItemTypeProvider(cache.Object, new Mock<IItemTypeCodec>().Object, factory.Object);

        Assert.Same(fallback, provider.Get(itemId));

        cache.Verify(current => current.Read(typeData.IndexId, archiveId, It.IsAny<int>()), Times.Never);
        factory.Verify(current => current.CreateType(itemId), Times.Once);
    }

    [Fact]
    public void NpcAndObjectGet_PreserveFactoryFallbackWhenParentArchiveIsAbsent()
    {
        const int npcId = 133;
        const int objectId = 259;
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(18)).Returns(ReferenceTableWithoutArchive(1));
        cache.Setup(current => current.ReadReferenceTable(16)).Returns(ReferenceTableWithoutArchive(1));
        var npcFallback = new Mock<INpcType>().Object;
        var objectFallback = new Mock<IObjectType>().Object;
        var npcFactory = new Mock<ITypeFactory<INpcType>>();
        npcFactory.Setup(current => current.CreateType(npcId)).Returns(npcFallback);
        var objectFactory = new Mock<ITypeFactory<IObjectType>>();
        objectFactory.Setup(current => current.CreateType(objectId)).Returns(objectFallback);
        var npcProvider = new NpcTypeProvider(cache.Object, new Mock<INpcTypeCodec>().Object, npcFactory.Object);
        var objectProvider = new ObjectTypeProvider(cache.Object, new Mock<IObjectTypeCodec>().Object, objectFactory.Object);

        Assert.Same(npcFallback, npcProvider.Get(npcId));
        Assert.Same(objectFallback, objectProvider.Get(objectId));

        cache.Verify(current => current.Read(18, 1, It.IsAny<int>()), Times.Never);
        cache.Verify(current => current.Read(16, 1, It.IsAny<int>()), Times.Never);
        npcFactory.Verify(current => current.CreateType(npcId), Times.Once);
        objectFactory.Verify(current => current.CreateType(objectId), Times.Once);
    }

    [Fact]
    public void ItemNpcAndObjectGet_FallBackForMissingChildMappings()
    {
        const int itemId = 259;
        const int npcId = 133;
        const int objectId = 259;
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(19)).Returns(ReferenceTableWithChild(1, 3, null).Object);
        cache.Setup(current => current.ReadReferenceTable(18)).Returns(ReferenceTableWithChild(1, 5, null).Object);
        cache.Setup(current => current.ReadReferenceTable(16)).Returns(ReferenceTableWithChild(1, 3, null).Object);
        var itemFallback = new Mock<IItemType>().Object;
        var npcFallback = new Mock<INpcType>().Object;
        var objectFallback = new Mock<IObjectType>().Object;
        var itemFactory = new Mock<ITypeFactory<IItemType>>();
        var npcFactory = new Mock<ITypeFactory<INpcType>>();
        var objectFactory = new Mock<ITypeFactory<IObjectType>>();
        itemFactory.Setup(current => current.CreateType(itemId)).Returns(itemFallback);
        npcFactory.Setup(current => current.CreateType(npcId)).Returns(npcFallback);
        objectFactory.Setup(current => current.CreateType(objectId)).Returns(objectFallback);
        var itemProvider = new ItemTypeProvider(cache.Object, new Mock<IItemTypeCodec>().Object, itemFactory.Object);
        var npcProvider = new NpcTypeProvider(cache.Object, new Mock<INpcTypeCodec>().Object, npcFactory.Object);
        var objectProvider = new ObjectTypeProvider(cache.Object, new Mock<IObjectTypeCodec>().Object, objectFactory.Object);

        Assert.Same(itemFallback, itemProvider.Get(itemId));
        Assert.Same(npcFallback, npcProvider.Get(npcId));
        Assert.Same(objectFallback, objectProvider.Get(objectId));

        cache.Verify(current => current.Read(19, 1, It.IsAny<int>()), Times.Never);
        cache.Verify(current => current.Read(18, 1, It.IsAny<int>()), Times.Never);
        cache.Verify(current => current.Read(16, 1, It.IsAny<int>()), Times.Never);
        itemFactory.Verify(current => current.CreateType(itemId), Times.Once);
        npcFactory.Verify(current => current.CreateType(npcId), Times.Once);
        objectFactory.Verify(current => current.CreateType(objectId), Times.Once);
    }

    [Fact]
    public void ItemGet_RealRelationshipHookResolvesRelatedItemsThroughTheOwningProvider()
    {
        const int itemId = 259;
        const int noteId = 2;
        const int noteTemplateId = 1;
        var table = ReferenceTableWithMappings((1, 3, 7), (0, noteId, 8), (0, noteTemplateId, 9));
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(19)).Returns(table.Object);
        var rootStream = new MemoryStreamSpy();
        var noteStream = new MemoryStreamSpy();
        var templateStream = new MemoryStreamSpy();
        cache.Setup(current => current.Read(19, 1, 7)).Returns(rootStream);
        cache.Setup(current => current.Read(19, 0, 8)).Returns(noteStream);
        cache.Setup(current => current.Read(19, 0, 9)).Returns(templateStream);

        var root = new Mock<IItemType>();
        root.SetupGet(current => current.NoteId).Returns(noteId);
        root.SetupGet(current => current.NoteTemplateId).Returns(noteTemplateId);
        root.SetupGet(current => current.LendTemplateId).Returns(-1);
        root.SetupGet(current => current.BoughtTemplateId).Returns(-1);
        var note = ItemWithoutRelationships();
        var template = ItemWithoutRelationships();
        var decodedById = new Dictionary<int, IItemType>
        {
            [itemId] = root.Object,
            [noteId] = note.Object,
            [noteTemplateId] = template.Object
        };
        var codec = new Mock<IItemTypeCodec>();
        codec.Setup(current => current.Decode(It.IsAny<int>(), It.IsAny<MemoryStream>()))
            .Returns((int typeId, MemoryStream _) => decodedById[typeId]);
        var provider = new ItemTypeProvider(cache.Object, codec.Object, new Mock<ITypeFactory<IItemType>>().Object, new ItemTypeEventHook());

        Assert.Same(root.Object, provider.Get(itemId));

        root.Verify(current => current.MakeNote(note.Object, template.Object), Times.Once);
        Assert.True(rootStream.IsDisposed);
        Assert.True(noteStream.IsDisposed);
        Assert.True(templateStream.IsDisposed);
        cache.Verify(current => current.Read(19, 1, 7), Times.Once);
        cache.Verify(current => current.Read(19, 0, 8), Times.Once);
        cache.Verify(current => current.Read(19, 0, 9), Times.Once);
    }

    [Fact]
    public void GenericGet_PropagatesMappedArchiveFailuresWithoutFactoryFallback()
    {
        const int typeId = 10;
        const int physicalMemberId = 3;
        var typeData = new QuestTypeData();
        var archiveId = typeData.GetArchiveId(typeId);
        var childId = typeData.GetArchiveEntryId(typeId);
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(2))
            .Returns(ReferenceTableWithMappedChild(archiveId, childId, physicalMemberId));
        cache.SetupSequence(current => current.Read(2, archiveId, physicalMemberId))
            .Throws<FileNotFoundException>()
            .Throws<InvalidDataException>();
        var factory = new Mock<ITypeFactory<IQuestType>>();
        var provider = new TypeProvider<IQuestType, QuestTypeData>(
            cache.Object,
            factory.Object,
            new Mock<ITypeCodec<IQuestType>>().Object);

        Assert.Throws<FileNotFoundException>(() => provider.Get(typeId));
        Assert.Throws<InvalidDataException>(() => provider.Get(typeId));

        cache.Verify(current => current.Read(2, archiveId, physicalMemberId), Times.Exactly(2));
        factory.Verify(current => current.CreateType(typeId), Times.Never);
    }

    [Fact]
    public void GenericGetRange_KeepsArchiveBatchReadAndSingleHookCall()
    {
        var cache = new Mock<ICacheAPI>();
        var table = ReferenceTableWithChild(35, 8, 3);
        table.Setup(current => current.GetEntry(35, 9)).Returns(CreateChild(5));
        cache.Setup(current => current.ReadReferenceTable(2)).Returns(table.Object);
        var archive = new Mock<IArchive>();
        using var firstMember = new MemoryStream([1]);
        using var secondMember = new MemoryStream([2]);
        archive.Setup(current => current.GetEntry(3)).Returns(firstMember);
        archive.Setup(current => current.GetEntry(5)).Returns(secondMember);
        cache.Setup(current => current.ReadArchive(2, 35)).Returns(archive.Object);
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
        cache.Verify(current => current.ReadArchive(2, 35), Times.Once);
        cache.Verify(current => current.Read(2, 35, It.IsAny<int>()), Times.Never);
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

    [Fact]
    public void SpriteGetRange_DisposesEarlierSpritesWhenLaterDecodeFails()
    {
        const int firstSpriteId = 5;
        const int secondSpriteId = 6;
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(8))
            .Returns(ReferenceTableWithMappings((firstSpriteId, 0, 13), (secondSpriteId, 0, 14)).Object);
        var firstStream = new MemoryStreamSpy();
        var secondStream = new MemoryStreamSpy();
        cache.Setup(current => current.Read(8, firstSpriteId, 13)).Returns(firstStream);
        cache.Setup(current => current.Read(8, secondSpriteId, 14)).Returns(secondStream);
        var firstSprite = new Mock<ISpriteType>();
        var decodingFailure = new InvalidDataException("corrupt sprite");
        var codec = new Mock<ISpriteTypeCodec>();
        codec.SetupSequence(current => current.Decode(It.IsAny<int>(), It.IsAny<MemoryStream>()))
            .Returns(firstSprite.Object)
            .Throws(decodingFailure);
        var provider = new SpriteTypeProvider(
            cache.Object,
            codec.Object,
            new Mock<ITypeFactory<ISpriteType>>().Object);

        var actualFailure = Assert.Throws<InvalidDataException>(() => provider.GetRange(firstSpriteId, secondSpriteId + 1));

        Assert.Same(decodingFailure, actualFailure);
        firstSprite.Verify(current => current.Dispose(), Times.Once);
        Assert.True(firstStream.IsDisposed);
        Assert.True(secondStream.IsDisposed);
    }

    private static Mock<IReferenceTable> ReferenceTableWithChild(int archiveId, int childId, int? physicalIndex)
    {
        var table = new Mock<IReferenceTable>();
        table.Setup(current => current.GetEntry(archiveId)).Returns(new Mock<IReferenceTableEntry>().Object);
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

    private static Mock<IReferenceTable> ReferenceTableWithMappings(params (int ArchiveId, int ChildId, int PhysicalIndex)[] mappings)
    {
        var table = new Mock<IReferenceTable>();
        foreach (var (archiveId, childId, physicalIndex) in mappings)
        {
            table.Setup(current => current.GetEntry(archiveId)).Returns(new Mock<IReferenceTableEntry>().Object);
            table.Setup(current => current.GetEntry(archiveId, childId)).Returns(CreateChild(physicalIndex));
        }

        return table;
    }

    private static Mock<IItemType> ItemWithoutRelationships()
    {
        var item = new Mock<IItemType>();
        item.SetupGet(current => current.NoteTemplateId).Returns(-1);
        item.SetupGet(current => current.LendTemplateId).Returns(-1);
        item.SetupGet(current => current.BoughtTemplateId).Returns(-1);
        return item;
    }

    private static ReferenceTable ReferenceTableWithoutArchive(int archiveId) =>
        new(7, 1, (ReferenceTableFlags)0, archiveId + 1);

    private static ReferenceTable ReferenceTableWithArchiveWithoutChild(int archiveId, int childId)
    {
        var table = ReferenceTableWithoutArchive(archiveId);
        var archive = new ReferenceTableEntry(archiveId);
        archive.InitializeEntries(childId + 1);
        table.AddEntry(archiveId, archive);
        return table;
    }

    private static ReferenceTable ReferenceTableWithMappedChild(int archiveId, int childId, int physicalIndex)
    {
        var table = ReferenceTableWithArchiveWithoutChild(archiveId, childId);
        table.GetEntry(archiveId)!.AddEntry(childId, new ReferenceTableChildEntry(physicalIndex));
        return table;
    }

    private static IReferenceTableChildEntry CreateChild(int physicalIndex)
    {
        var child = new Mock<IReferenceTableChildEntry>();
        child.SetupGet(current => current.Index).Returns(physicalIndex);
        return child.Object;
    }
}
