using Hagalaz.Data.Entities;
using Hagalaz.Data.Users;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class PasswordHasherSecurityTests
    {
        [Fact]
        public void VerifyHashedPassword_WithPlaintextPassword_ShouldFail()
        {
            // Arrange
            var hasher = new HagalazPasswordHasher();
            var user = new Character { Email = "test@example.com" };
            var password = "SecurePassword123";

            // Act
            var result = hasher.VerifyHashedPassword(user, password, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void HashPassword_ShouldBeNonDeterministic_WithSalt()
        {
            // Arrange
            var hasher = new HagalazPasswordHasher();
            var user = new Character { Email = "test@example.com" };
            var password = "SecurePassword123";

            // Act
            var hash1 = hasher.HashPassword(user, password);
            var hash2 = hasher.HashPassword(user, password);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void VerifyHashedPassword_WithCorrectPassword_ShouldSucceed()
        {
            // Arrange
            var hasher = new HagalazPasswordHasher();
            var user = new Character { Email = "test@example.com" };
            var password = "SecurePassword123";
            var hash = hasher.HashPassword(user, password);

            // Act
            var result = hasher.VerifyHashedPassword(user, hash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithLegacySha256Hash_ShouldSucceedWithRehashNeeded()
        {
            // Arrange
            var hasher = new HagalazPasswordHasher();
            var user = new Character { Email = "test@example.com" };
            var password = "SecurePassword123";
            // Manually compute legacy hash: SHA256("test@example.comSecurePassword123")
            var legacyHash = Hagalaz.Security.HashHelper.ComputeHash(user.Email + password, HashType.SHA256);

            // Act
            var result = hasher.VerifyHashedPassword(user, legacyHash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }
    }
}
