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
    }
}