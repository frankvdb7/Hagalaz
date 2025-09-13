using Hagalaz.Cache.Utilities;
using System.Text;
using Xunit;

namespace Hagalaz.Cache.Tests.Utilities
{
    public class WhirlpoolTests
    {
        [Fact]
        public void Whirlpool_EmptyString_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = "";
            var expectedHash = "19FA61D75522A4669B44E39C1D2E1726C530232130D407F89AFEE0964997F7A73E83BE698B288FEBCF88E3E03C4F0757EA8964E59B63D93708B138CC42A66EB3";

            // Act
            var data = Encoding.UTF8.GetBytes(input);
            var result = Whirlpool.GenerateDigest(data, 0, data.Length);
            var actualHash = BitConverter.ToString(result).Replace("-", "");

            // Assert
            Assert.Equal(expectedHash, actualHash, ignoreCase: true);
        }

        [Fact]
        public void Whirlpool_QuickBrownFox_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = "The quick brown fox jumps over the lazy dog";
            var expectedHash = "B97DE512E91E3828B40D2B0FDCE9CEB3C4A71F9BEA8D88E75C4FA854DF36725FD2B52EB6544EDCACD6F8BEDDFEA403CB55AE31F03AD62A5EF54E42EE82C3FB35";

            // Act
            var data = Encoding.UTF8.GetBytes(input);
            var result = Whirlpool.GenerateDigest(data, 0, data.Length);
            var actualHash = BitConverter.ToString(result).Replace("-", "");

            // Assert
            Assert.Equal(expectedHash, actualHash, ignoreCase: true);
        }
    }
}
