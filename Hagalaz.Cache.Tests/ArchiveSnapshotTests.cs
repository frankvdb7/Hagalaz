using System.IO;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class ArchiveSnapshotTests
{
    [Fact]
    public void Create_CopiesMembersFromSingleAndMultiChunkArchiveDecoderOutput()
    {
        var singleBytes = new byte[] { 4, 8, 15 };
        var single = DecodeSnapshot(singleBytes, 1);
        using (var member = single.OpenMember(0))
        {
            Assert.Equal(singleBytes, member.ToArray());
        }

        var first = new byte[] { 1, 2, 3 };
        var second = new byte[] { 4, 5, 6, 7 };
        var multiple = DecodeSnapshot(ArchiveTestData.EncodeArchive(first, second), 2);
        using var firstMember = multiple.OpenMember(0);
        using var secondMember = multiple.OpenMember(1);
        Assert.Equal(first, firstMember.ToArray());
        Assert.Equal(second, secondMember.ToArray());
    }

    [Fact]
    public void Create_CopiesNegativeFooterDeltaAndMultipleChunks()
    {
        using var negativeFooter = new MemoryStream();
        negativeFooter.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7 });
        negativeFooter.Write(new byte[] { 8, 9, 10 });
        negativeFooter.WriteInt(7);
        negativeFooter.WriteInt(-4);
        negativeFooter.WriteByte(1);
        var negativeSnapshot = DecodeSnapshot(negativeFooter.ToArray(), 2);

        using var first = negativeSnapshot.OpenMember(0);
        using var second = negativeSnapshot.OpenMember(1);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7 }, first.ToArray());
        Assert.Equal(new byte[] { 8, 9, 10 }, second.ToArray());

        using var multipleChunks = new MemoryStream();
        multipleChunks.Write(new byte[] { 1, 2 });
        multipleChunks.Write(new byte[] { 3, 4, 5 });
        multipleChunks.Write(new byte[] { 6 });
        multipleChunks.Write(new byte[] { 7, 8, 9, 10 });
        multipleChunks.WriteInt(2);
        multipleChunks.WriteInt(1);
        multipleChunks.WriteInt(1);
        multipleChunks.WriteInt(3);
        multipleChunks.WriteByte(2);
        var chunkSnapshot = DecodeSnapshot(multipleChunks.ToArray(), 2);

        using var chunkFirst = chunkSnapshot.OpenMember(0);
        using var chunkSecond = chunkSnapshot.OpenMember(1);
        Assert.Equal(new byte[] { 1, 2, 6 }, chunkFirst.ToArray());
        Assert.Equal(new byte[] { 3, 4, 5, 7, 8, 9, 10 }, chunkSecond.ToArray());
    }

    [Fact]
    public void Create_PreservesEmptyPhysicalMembersAndAccountsForTheirDescriptors()
    {
        var snapshot = DecodeSnapshot(ArchiveTestData.EncodeArchive([], [8, 9]), 2);

        using var empty = snapshot.OpenMember(0);
        using var nonEmpty = snapshot.OpenMember(1);
        Assert.Equal(0, empty.Length);
        Assert.Equal(new byte[] { 8, 9 }, nonEmpty.ToArray());
        Assert.Equal(2 + (2 * sizeof(int) * 2) + 128, snapshot.RetentionWeight);
    }

    [Fact]
    public void OpenMember_ReturnsIndependentReadOnlyStreamsAndKeepsSnapshotAfterDisposal()
    {
        var snapshot = DecodeSnapshot(ArchiveTestData.EncodeArchive([10, 11, 12], [20, 21]), 2);

        using var first = snapshot.OpenMember(0);
        Assert.Equal(0, first.Position);
        Assert.True(first.CanRead);
        Assert.True(first.CanSeek);
        Assert.False(first.CanWrite);
        Assert.False(first.TryGetBuffer(out _));
        Assert.Throws<UnauthorizedAccessException>(() => first.GetBuffer());
        Assert.Throws<NotSupportedException>(() => first.WriteByte(1));

        first.Position = 1;
        Assert.Equal(11, first.ReadByte());

        using var second = snapshot.OpenMember(0);
        Assert.Equal(0, second.Position);
        Assert.Equal(10, second.ReadByte());
        first.Dispose();

        using var afterDisposal = snapshot.OpenMember(0);
        Assert.Equal(new byte[] { 10, 11, 12 }, afterDisposal.ToArray());
    }

    [Fact]
    public void OpenMember_RejectsMissingPhysicalIndex()
    {
        var snapshot = DecodeSnapshot(new byte[] { 1 }, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.OpenMember(1));
    }

    private static ArchiveSnapshot DecodeSnapshot(byte[] archiveBytes, int memberCount)
    {
        using var containerData = new MemoryStream(archiveBytes);
        using var container = new Container(containerData);
        using var archive = new ArchiveDecoder().Decode(container, memberCount);
        return ArchiveSnapshot.Create(archive);
    }

}
