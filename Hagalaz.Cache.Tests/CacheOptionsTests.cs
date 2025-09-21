using Xunit;

namespace Hagalaz.Cache.Tests;

public class CacheOptionsTests
{
    [Fact]
    public void Path_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new CacheOptions();
        var expectedPath = "/test/path";

        // Act
        options.Path = expectedPath;
        var actualPath = options.Path;

        // Assert
        Assert.Equal(expectedPath, actualPath);
    }
}
