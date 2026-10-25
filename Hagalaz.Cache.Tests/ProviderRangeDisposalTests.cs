using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Providers;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class ProviderRangeDisposalTests
{
    [Fact]
    public void GenericRange_DisposesArchivesOnSwitchAndAfterRangeHook()
    {
        var fixture = CreateTwoArchiveFixture(20);
        var firstType = new Mock<IQuestType>().Object;
        var secondType = new Mock<IQuestType>().Object;
        var codec = new Mock<ITypeCodec<IQuestType>>();
        codec.Setup(current => current.Decode(127, fixture.FirstMember)).Returns(firstType);
        codec.Setup(current => current.Decode(128, fixture.SecondMember)).Returns(secondType);
        var hook = new Mock<ITypeEventHook<IQuestType>>();
        var provider = new TypeProvider<IQuestType, AnimationTypeData>(
            fixture.Cache.Object,
            new Mock<ITypeFactory<IQuestType>>().Object,
            codec.Object,
            hook.Object);

        var result = provider.GetRange(127, 129);

        AssertSuccessfulRange(result, new[] { firstType, secondType }, fixture, provider, hook);
    }

    [Fact]
    public void NpcRange_DisposesCurrentAndPreviousArchivesWhenDecodingThrows()
    {
        var fixture = CreateTwoArchiveFixture(18);
        var failure = new InvalidDataException("corrupt NPC definition");
        var codec = new Mock<INpcTypeCodec>();
        codec.Setup(current => current.Decode(127, fixture.FirstMember)).Returns(new Mock<INpcType>().Object);
        codec.Setup(current => current.Decode(128, fixture.SecondMember)).Throws(failure);
        var hook = new Mock<ITypeEventHook<INpcType>>();
        var provider = new NpcTypeProvider(
            fixture.Cache.Object,
            codec.Object,
            new Mock<ITypeFactory<INpcType>>().Object,
            hook.Object);

        var actual = Assert.Throws<InvalidDataException>(() => provider.GetRange(127, 129));

        Assert.Same(failure, actual);
        AssertArchivesDisposed(fixture);
        hook.Verify(current => current.AfterDecode(provider, It.IsAny<INpcType[]>()), Times.Never);
    }

    [Fact]
    public void NpcRange_PreservesBatchedResultsAndHookFrequencyWhileDisposingArchives()
    {
        var fixture = CreateTwoArchiveFixture(18);
        var firstType = new Mock<INpcType>().Object;
        var secondType = new Mock<INpcType>().Object;
        var codec = new Mock<INpcTypeCodec>();
        codec.Setup(current => current.Decode(127, fixture.FirstMember)).Returns(firstType);
        codec.Setup(current => current.Decode(128, fixture.SecondMember)).Returns(secondType);
        var hook = new Mock<ITypeEventHook<INpcType>>();
        var provider = new NpcTypeProvider(
            fixture.Cache.Object,
            codec.Object,
            new Mock<ITypeFactory<INpcType>>().Object,
            hook.Object);

        var result = provider.GetRange(127, 129);

        AssertSuccessfulRange(result, new[] { firstType, secondType }, fixture, provider, hook);
    }

    [Fact]
    public void GenericRange_DisposesPreviousArchiveWhenNextArchiveLoadThrows()
    {
        var fixture = CreateTwoArchiveFixture(20);
        var failure = new FileNotFoundException("next archive missing");
        fixture.Cache.Setup(current => current.ReadArchive(20, 1)).Throws(failure);
        var codec = new Mock<ITypeCodec<IQuestType>>();
        codec.Setup(current => current.Decode(127, fixture.FirstMember)).Returns(new Mock<IQuestType>().Object);
        var provider = new TypeProvider<IQuestType, AnimationTypeData>(
            fixture.Cache.Object,
            new Mock<ITypeFactory<IQuestType>>().Object,
            codec.Object);

        var actual = Assert.Throws<FileNotFoundException>(() => provider.GetRange(127, 129));

        Assert.Same(failure, actual);
        fixture.FirstArchive.Verify(current => current.Dispose(), Times.Once);
        Assert.True(fixture.FirstMember.IsDisposed);
    }

    [Fact]
    public void GenericRange_DisposesFinalArchiveWhenRangeHookThrows()
    {
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(20)).Returns(ReferenceTableWithMappings((0, 0, 3)));
        var member = new MemoryStreamSpy();
        var archive = ArchiveWithMember(3, member);
        cache.Setup(current => current.ReadArchive(20, 0)).Returns(archive.Object);
        var codec = new Mock<ITypeCodec<IQuestType>>();
        codec.Setup(current => current.Decode(0, member)).Returns(new Mock<IQuestType>().Object);
        var hook = new Mock<ITypeEventHook<IQuestType>>();
        var failure = new InvalidOperationException("range hook failed");
        hook.Setup(current => current.AfterDecode(It.IsAny<ITypeProvider<IQuestType>>(), It.IsAny<IQuestType[]>()))
            .Throws(failure);
        var provider = new TypeProvider<IQuestType, AnimationTypeData>(
            cache.Object,
            new Mock<ITypeFactory<IQuestType>>().Object,
            codec.Object,
            hook.Object);

        var actual = Assert.Throws<InvalidOperationException>(() => provider.GetRange(0, 1));

        Assert.Same(failure, actual);
        archive.Verify(current => current.Dispose(), Times.Once);
        Assert.True(member.IsDisposed);
    }

    private static Mock<IArchive> ArchiveWithMember(int physicalIndex, MemoryStreamSpy member)
    {
        var archive = new Mock<IArchive>();
        archive.Setup(current => current.GetEntry(physicalIndex)).Returns(member);
        archive.Setup(current => current.Dispose()).Callback(member.Dispose);
        return archive;
    }

    private static RangeArchiveFixture CreateTwoArchiveFixture(int indexId)
    {
        var cache = new Mock<ICacheAPI>();
        cache.Setup(current => current.ReadReferenceTable(indexId)).Returns(ReferenceTableWithMappings((0, 127, 3), (1, 0, 4)));
        var firstMember = new MemoryStreamSpy();
        var secondMember = new MemoryStreamSpy();
        var firstArchive = ArchiveWithMember(3, firstMember);
        var secondArchive = ArchiveWithMember(4, secondMember);
        cache.Setup(current => current.ReadArchive(indexId, 0)).Returns(firstArchive.Object);
        cache.Setup(current => current.ReadArchive(indexId, 1)).Returns(secondArchive.Object);
        return new RangeArchiveFixture(cache, firstMember, secondMember, firstArchive, secondArchive);
    }

    private static void AssertSuccessfulRange<T>(
        T[] actual,
        T[] expected,
        RangeArchiveFixture fixture,
        ITypeProvider<T> provider,
        Mock<ITypeEventHook<T>> hook)
        where T : IType
    {
        Assert.Equal(expected, actual);
        AssertArchivesDisposed(fixture);
        hook.Verify(current => current.AfterDecode(provider, It.Is<T[]>(types => types.Length == 2)), Times.Once);
    }

    private static void AssertArchivesDisposed(RangeArchiveFixture fixture)
    {
        fixture.FirstArchive.Verify(current => current.Dispose(), Times.Once);
        fixture.SecondArchive.Verify(current => current.Dispose(), Times.Once);
        Assert.True(fixture.FirstMember.IsDisposed);
        Assert.True(fixture.SecondMember.IsDisposed);
    }

    private static IReferenceTable ReferenceTableWithMappings(params (int ArchiveId, int ChildId, int PhysicalIndex)[] mappings)
    {
        var table = new Mock<IReferenceTable>();
        foreach (var (archiveId, childId, physicalIndex) in mappings)
        {
            table.Setup(current => current.GetEntry(archiveId)).Returns(new Mock<IReferenceTableEntry>().Object);
            var child = new Mock<IReferenceTableChildEntry>();
            child.SetupGet(current => current.Index).Returns(physicalIndex);
            table.Setup(current => current.GetEntry(archiveId, childId)).Returns(child.Object);
        }

        return table.Object;
    }

    private sealed record RangeArchiveFixture(
        Mock<ICacheAPI> Cache,
        MemoryStreamSpy FirstMember,
        MemoryStreamSpy SecondMember,
        Mock<IArchive> FirstArchive,
        Mock<IArchive> SecondArchive);
}
