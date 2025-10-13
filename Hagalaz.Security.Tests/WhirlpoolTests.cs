using System.Linq;
using System.Text;
using Xunit;
using System;

namespace Hagalaz.Security.Tests
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
            var actualHash = Convert.ToHexString(result);

            // Assert
            Assert.Equal(expectedHash, actualHash, ignoreCase: true);
        }

        [Fact]
        public void Whirlpool_LongString_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = new string('a', 1000000);
            var expectedHash = "0C99005BEB57EFF50A7CF005560DDF5D29057FD86B20BFD62DECA0F1CCEA4AF51FC15490EDDC47AF32BB2B66C34FF9AD8C6008AD677F77126953B226E4ED8B01";

            // Act
            var data = Encoding.UTF8.GetBytes(input);
            var result = Whirlpool.GenerateDigest(data, 0, data.Length);
            var actualHash = Convert.ToHexString(result);

            // Assert
            Assert.Equal(expectedHash, actualHash, ignoreCase: true);
        }

        [Fact]
        public void Whirlpool_AllByteValues_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
            var expectedHash = "05A308887B2392BFB3C71A438AA03153CA102B62CA9F5CBB4AC2D7F161C9D7F8BC6EB895CB2BE5F595C656C24C50F1E293F37C7B5B07F32BAF251DFE11B4B2A3";

            // Act
            var result = Whirlpool.GenerateDigest(input, 0, input.Length);
            var actualHash = Convert.ToHexString(result);

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
            var actualHash = Convert.ToHexString(result);

            // Assert
            Assert.Equal(expectedHash, actualHash, ignoreCase: true);
        }
    }
}