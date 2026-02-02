using Hagalaz.Data.Entities;
using Hagalaz.Data.Users;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class HagalazPasswordHasherTests
    {
        private readonly HagalazPasswordHasher _hasher;

        public HagalazPasswordHasherTests()
        {
            _hasher = new HagalazPasswordHasher();
        }

        [Fact]
        public void HashPassword_ReturnsV3Hash()
        {
            // Arrange
            var user = new Character { Email = "test@example.com" };
            var password = "Password123!";

            // Act
            var hash = _hasher.HashPassword(user, password);

            // Assert
            Assert.StartsWith("AQAAAA", hash);
        }

        [Fact]
        public void VerifyHashedPassword_LegacySha256_ReturnsSuccessRehashNeeded()
        {
            // Arrange
            var user = new Character { Email = "test@example.com" };
            var password = "Password123!";
            // Compute legacy hash: SHA256(Email + password)
            var legacyHash = HashHelper.ComputeHash(user.Email + password, HashType.SHA256);

            // Act
            var result = _hasher.VerifyHashedPassword(user, legacyHash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_ModernHash_ReturnsSuccess()
        {
            // Arrange
            var user = new Character { Email = "test@example.com" };
            var password = "Password123!";
            var modernHash = _hasher.HashPassword(user, password);

            // Act
            var result = _hasher.VerifyHashedPassword(user, modernHash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_Plaintext_ReturnsFailed()
        {
            // Arrange
            var user = new Character { Email = "test@example.com" };
            var password = "Password123!";
            var plaintextHash = password;

            // Act
            var result = _hasher.VerifyHashedPassword(user, plaintextHash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void VerifyHashedPassword_WrongPassword_ReturnsFailed()
        {
            // Arrange
            var user = new Character { Email = "test@example.com" };
            var password = "Password123!";
            var hash = _hasher.HashPassword(user, password);

            // Act
            var result = _hasher.VerifyHashedPassword(user, hash, "WrongPassword");

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }
}
