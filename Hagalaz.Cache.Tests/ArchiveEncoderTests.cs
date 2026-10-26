using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class ArchiveEncoderTests
{
    [Fact]
    public void Encode_SingleMember_ReturnsExactBytes()
    {
        var member = new byte[] { 1, 2, 3 };
        using var container = new Container(new MemoryStream(member));
        using var archive = new ArchiveDecoder().Decode(container, 1);

        using var encoded = new ArchiveEncoder().Encode(archive);

        Assert.Equal(member, encoded.ToArray());
    }

    [Fact]
    public void Encode_MultipleMembers_RoundTripsThroughArchiveDecoder()
    {
        var firstMember = new byte[] { 1, 2 };
        var secondMember = new byte[] { 3 };
        var sourceData = ArchiveTestData.EncodeArchive(firstMember, secondMember);
        using var sourceContainer = new Container(new MemoryStream(sourceData));
        using var archive = new ArchiveDecoder().Decode(sourceContainer, 2);

        using var encoded = new ArchiveEncoder().Encode(archive);
        Assert.Equal(sourceData, encoded.ToArray());

        using var encodedContainer = new Container(new MemoryStream(encoded.ToArray()));
        using var decoded = new ArchiveDecoder().Decode(encodedContainer, 2);
        Assert.Equal(firstMember, decoded.GetEntry(0).ToArray());
        Assert.Equal(secondMember, decoded.GetEntry(1).ToArray());
    }
}
