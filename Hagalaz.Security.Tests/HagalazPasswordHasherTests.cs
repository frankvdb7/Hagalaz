using Hagalaz.Data.Entities;
using Hagalaz.Data.Users;
using Hagalaz.Security;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class HagalazPasswordHasherTests
    {
        private readonly HagalazPasswordHasher _hasher = new();
        private readonly Character _user = new() { Email = "test@example.com", UserName = "testuser" };

        [Fact]
        public void HashPassword_ShouldUseV3Format()
        {
            // Arrange
            var password = "Password123!";

            // Act
            var result = _hasher.HashPassword(_user, password);

            // Assert
            // Identity V3 hashes start with AQAAAA
            Assert.StartsWith("AQAAAA", result);
        }

        [Fact]
        public void VerifyHashedPassword_V3Hash_ShouldSucceed()
        {
            // Arrange
            var password = "Password123!";
            var hash = _hasher.HashPassword(_user, password);

            // Act
            var result = _hasher.VerifyHashedPassword(_user, hash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_LegacySHA256_ShouldSucceedRehashNeeded()
        {
            // Arrange
            var password = "Password123!";
            var legacyHash = HashHelper.ComputeHash(_user.Email + password, HashType.SHA256);

            // Act
            var result = _hasher.VerifyHashedPassword(_user, legacyHash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_Plaintext_ShouldSucceedRehashNeeded()
        {
            // Arrange
            var password = "Password123!";
            var legacyHash = password;

            // Act
            var result = _hasher.VerifyHashedPassword(_user, legacyHash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_WrongPassword_ShouldFail()
        {
            // Arrange
            var password = "Password123!";
            var legacyHash = HashHelper.ComputeHash(_user.Email + password, HashType.SHA256);
            var wrongPassword = "WrongPassword";

            // Act
            var result = _hasher.VerifyHashedPassword(_user, legacyHash, wrongPassword);

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void VerifyHashedPassword_InvalidHash_ShouldFail(string? invalidHash)
        {
            // Act
            var result = _hasher.VerifyHashedPassword(_user, invalidHash!, "anyPassword");

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }
}
