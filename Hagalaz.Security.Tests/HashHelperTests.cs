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
        [InlineData(HashType.MD5, "你好世界", "65396ee4aad0b4f17aacd1c6112ee364")]
        [InlineData(HashType.SHA1, "你好世界", "dabaa5fe7c47fb21be902480a13013f16a1ab6eb")]
        [InlineData(HashType.SHA256, "你好世界", "beca6335b20ff57ccc47403ef4d9e0b8fccb4442b3151c2e7d50050673d43172")]
        [InlineData(HashType.SHA384, "你好世界", "5621250177cc297c02d4c7c2a950d541a52b5c478e1fa16ca5022316de898d7be5c66b16ad735295b48b84a47e986144")]
        [InlineData(HashType.SHA512, "你好世界", "4b28a152c8e203ebb52e099301041e3cf704a56190d3097ec8b086a0f9bfb4b9d533ce71fc3bcf374359e506dc5f17322ec3911eac8dd8f5b35308d938ba0c26")]
        public void ComputeHash_UnicodeString_ReturnsCorrectHash(HashType hashType, string input, string expected)
        {
            // Act
            var result = HashHelper.ComputeHash(input, hashType);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(HashType.MD5, "cabe45dcc9ae5b66ba86600cca6b8ba8")]
        [InlineData(HashType.SHA1, "291e9a6c66994949b57ba5e650361e98fc36b1ba")]
        [InlineData(HashType.SHA256, "41edece42d63e8d9bf515a9ba6932e1c20cbc9f5a5d134645adb5db1b9737ea3")]
        [InlineData(HashType.SHA384, "f54480689c6b0b11d0303285d9a81b21a93bca6ba5a1b4472765dca4da45ee328082d469c650cd3b61b16d3266ab8ced")]
        [InlineData(HashType.SHA512, "67ba5535a46e3f86dbfbed8cbbaf0125c76ed549ff8b0b9e03e0c88cf90fa634fa7b12b47d77b694de488ace8d9a65967dc96df599727d3292a8d9d447709c97")]
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
        [InlineData(HashType.MD5, "`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?", "8216936a3435874180d9e5f0bc9caec2")]
        [InlineData(HashType.SHA1, "`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?", "108f0d27764f61210b1986c07b47933c75351cd0")]
        [InlineData(HashType.SHA256, "`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?", "e00f877d7aed70781cb3a80ffc715b3fc03a56e8eacf178bf5a7ebf22c0e0422")]
        [InlineData(HashType.SHA384, "`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?", "fee86c4374318a53fd67595a124414ce639266ea7db81d9b0579749acd3a57638642692d5b48cb313572c663edb8bab6")]
        [InlineData(HashType.SHA512, "`~1!2@3#4$5%6^7&8*9(0)-_=+[]{}\\|;:'\",<.>/?", "fc35a5499b35ef7abb5a573836b2620478c2567b23a41c7956426ac3542cd27fec2928019f6ad43077f7978802a2853474591864e05d98b9f96dfcd3bcf4745d")]
        public void ComputeHash_SpecialCharacters_ReturnsCorrectHash(HashType hashType, string input, string expected)
        {
            // Act
            var result = HashHelper.ComputeHash(input, hashType);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
