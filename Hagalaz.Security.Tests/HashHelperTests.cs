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
    }
}