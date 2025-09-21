using System.IO;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class ArchiveTests
{
    [Fact]
    public void GetEntry_ReturnsCorrectEntry()
    {
        // Arrange
        var archive = new Archive(1);
        var expectedStream = new MemoryStream(new byte[] { 1, 2, 3 });
        archive.Entries[0] = expectedStream;

        // Act
        var actualStream = archive.GetEntry(0);

        // Assert
        Assert.Equal(expectedStream, actualStream);
    }
}
