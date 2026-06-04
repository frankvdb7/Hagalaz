using Hagalaz.Cache.Abstractions.Types.Providers;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class DecodePartRequestTests
    {
        [Fact]
        public void Properties_AreInitializable()
        {
            // Arrange
            var request = new DecodePartRequest();

            // Act
            request.RegionID = 123;
            request.XteaKeys = new int[] { 1, 2, 3, 4 };

            // Assert
            Assert.Equal(123, request.RegionID);
            Assert.Equal(new int[] { 1, 2, 3, 4 }, request.XteaKeys);
        }
    }
}
