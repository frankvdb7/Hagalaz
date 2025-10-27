using System.Security.Cryptography;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class HashHelperTests
    {
        [Theory]
        [InlineData(HashType.MD5)]
        [InlineData(HashType.SHA1)]
        [InlineData(HashType.SHA256)]
        [InlineData(HashType.SHA384)]
        [InlineData(HashType.SHA512)]
        public void TestComputeHash(HashType hashType)
        {
            // Arrange
            var data = "test";

            // Act
            var result = HashHelper.ComputeHash(data, hashType);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(data, result);
        }

        [Theory]
        [InlineData(HashType.MD5, typeof(MD5))]
        [InlineData(HashType.SHA1, typeof(SHA1))]
        [InlineData(HashType.SHA256, typeof(SHA256))]
        [InlineData(HashType.SHA384, typeof(SHA384))]
        [InlineData(HashType.SHA512, typeof(SHA512))]
        public void CreateNewInstance_ValidHashType_ReturnsInstance(HashType hashType, System.Type expectedType)
        {
            // Act
            var result = HashHelper.CreateNewInstance(hashType);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom(expectedType, result);
        }

        [Fact]
        public void CreateNewInstance_InvalidHashType_ReturnsNull()
        {
            // Act
            var result = HashHelper.CreateNewInstance((HashType)99);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("test", HashType.MD5, "098f6bcd4621d373cade4e832627b4f6")]
        [InlineData("test", HashType.SHA1, "a94a8fe5ccb19ba61c4c0873d391e987982fbbd3")]
        [InlineData("test", HashType.SHA256, "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
        [InlineData("test", HashType.SHA384, "768412320f7b0aa5812fce428dc4706b3cae50e02a64caa16a782249bfe8efc4b7ef1ccb126255d196047dfedf17a0a9")]
        [InlineData("test", HashType.SHA512, "ee26b0dd4af7e749aa1a8ee3c10ae9923f618980772e473f8819a5d4940e0db27ac185f8a0e1d5f84f88bc887fd67b143732c304cc5fa9ad8e6f57f50028a8ff")]
        public void ComputeHash_ReturnsCorrectHash(string input, HashType hashType, string expected)
        {
            // Act
            var result = HashHelper.ComputeHash(input, hashType);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(HashType.MD5, "你好世界", "c92d525281534a26f8c2b7121b662f59")]
        [InlineData(HashType.SHA1, "你好世界", "e223b5d3a58e858739956403c20c02559b132890")]
        [InlineData(HashType.SHA256, "你好世界", "5113d8b022137c4d28437f19e4878a291e6b3776e65b2636d93ba4f4d2f1f452")]
        [InlineData(HashType.SHA384, "你好世界", "e6b60e6181f21396a928956973347c61f74fc6e7a57a15998a139e14a1f11a43a0166c4333b5c207d5b10787b4ab3e35")]
        [InlineData(HashType.SHA512, "你好世界", "3c3fae3221e7d23e5939763529b53361834b953457a86169c9b68c242a58b68a48914659f13b1b36f78479e0a05a1e2f8c5b010f36f2f9f5926c2e2832822a16")]
        public void ComputeHash_UnicodeString_ReturnsCorrectHash(HashType hashType, string input, string expected)
        {
            // Act
            var result = HashHelper.ComputeHash(input, hashType);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(HashType.MD5, "77a513180231b03e0a1a5b674843d726")]
        [InlineData(HashType.SHA1, "04f83b1a21d37d38b553aca11e1e0d021c1a938c")]
        [InlineData(HashType.SHA256, "1f40fc92da241694750979ee6cf582f2d5d7d28e18335de05abc54d0560e0f53")]
        [InlineData(HashType.SHA384, "35d6a2f39c3e62df301b1a7b137f6a7384a4a5b36585121b6a5e125c11648a7313a52c33a901007e997a06410940eda6")]
        [InlineData(HashType.SHA512, "1233fd4a4603b22a07c13483f3e091b3558ef6145396f53e6b3614c5d5e5b38f1a30f1d438c645f7881c6185856779435889be82161b40285a4993883a6b0cfc")]
        public void ComputeHash_LongString_ReturnsCorrectHash(HashType hashType, string expected)
        {
            // Arrange
            var input = new string('a', 1000);

            // Act
            var result = HashHelper.ComputeHash(input, hashType);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(HashType.MD5, "d41d8cd98f00b204e9800998ecf8427e")]
        [InlineData(HashType.SHA1, "da39a3ee5e6b4b0d3255bfef95601890afd80709")]
        [InlineData(HashType.SHA256, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [InlineData(HashType.SHA384, "38b060a751ac96384cd9327eb1b1e36a21fdb71114be07434c0cc7bf63f6e1da274edebfe76f65fbd51ad2f14898b95b")]
        [InlineData(HashType.SHA512, "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e")]
        public void ComputeHash_EmptyString_ReturnsCorrectHash(HashType hashType, string expected)
        {
            // Act
            var result = HashHelper.ComputeHash(string.Empty, hashType);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(HashType.MD5, "`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?", "a1d37c568d708a9459a3e3ed754546b5")]
        [InlineData(HashType.SHA1, "`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?", "d29a552523e1f72921762118d0ef72997453e00e")]
        [InlineData(HashType.SHA256, "`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?", "81255562f79036a105c3127ad15a9930e461bdfa6a1660f9942a784d14b434b9")]
        [InlineData(HashType.SHA384, "`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?", "f2e8250b73c448578b5e679a78a635293299a91a92a54a4855268c121ca493010376180a30b8c6e3d25666c04f982885")]
        [InlineData(HashType.SHA512, "`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?", "b336e1c0715975d9e5a329623e839e530939523a1a721669d6ea4f9103295c654378f85f1c73a118029584284b3602d131f3f4e245e317e335555d28892f33c3")]
        public void ComputeHash_SpecialCharacters_ReturnsCorrectHash(HashType hashType, string input, string expected)
        {
            // Act
            var result = HashHelper.ComputeHash(input, hashType);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}