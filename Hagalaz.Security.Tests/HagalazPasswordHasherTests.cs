using Hagalaz.Data.Entities;
using Hagalaz.Data.Users;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class HagalazPasswordHasherTests
    {
        private readonly HagalazPasswordHasher _hasher = new();
        private readonly Character _user = new("testuser") { Email = "test@example.com" };

        [Fact]
        public void HashPassword_ShouldUseModernHasher()
        {
            // Arrange
            var password = "Password123!";

            // Act
            var hash = _hasher.HashPassword(_user, password);

            // Assert
            Assert.StartsWith("AQAAAA", hash); // ASP.NET Core Identity PBKDF2 hashes start with version marker (usually 'AQAAAA')
        }

        [Fact]
        public void VerifyHashedPassword_ModernHash_ShouldSucceed()
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
        public void VerifyHashedPassword_LegacySHA256Hash_ShouldSucceedWithRehashNeeded()
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
        public void VerifyHashedPassword_Plaintext_ShouldSucceedWithRehashNeeded()
        {
            // Arrange
            var password = "Password123!";

            // Act
            var result = _hasher.VerifyHashedPassword(_user, password, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_InvalidPassword_ShouldFail()
        {
            // Arrange
            var password = "Password123!";
            var hash = _hasher.HashPassword(_user, password);

            // Act
            var result = _hasher.VerifyHashedPassword(_user, hash, "WrongPassword!");

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void VerifyHashedPassword_InvalidLegacyHash_ShouldFail()
        {
            // Arrange
            var password = "Password123!";
            var legacyHash = HashHelper.ComputeHash(_user.Email + password, HashType.SHA256);

            // Act
            var result = _hasher.VerifyHashedPassword(_user, legacyHash, "WrongPassword!");

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }
}
