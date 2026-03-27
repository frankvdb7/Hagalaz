using Hagalaz.Data.Entities;
using Hagalaz.Data.Users;
using Hagalaz.Security;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class PasswordHasherCompatibilityTests
    {
        private readonly HagalazPasswordHasher _hasher = new();
        private readonly Character _testUser = new() { Email = "test@example.com" };

        [Fact]
        public void VerifyHashedPassword_WithPlaintext_ReturnsSuccessRehashNeeded()
        {
            // Arrange
            string password = "StrongPassword123!";
            string storedPassword = password; // Legacy plaintext

            // Act
            var result = _hasher.VerifyHashedPassword(_testUser, storedPassword, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithLegacySHA256_ReturnsSuccessRehashNeeded()
        {
            // Arrange
            string password = "StrongPassword123!";
            string storedPassword = HashHelper.ComputeHash(_testUser.Email + password, HashType.SHA256);

            // Act
            var result = _hasher.VerifyHashedPassword(_testUser, storedPassword, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithNewPBKDF2_ReturnsSuccess()
        {
            // Arrange
            string password = "StrongPassword123!";
            string hashedPassword = _hasher.HashPassword(_testUser, password);

            // Act
            var result = _hasher.VerifyHashedPassword(_testUser, hashedPassword, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithWrongPassword_ReturnsFailed()
        {
            // Arrange
            string password = "StrongPassword123!";
            string wrongPassword = "WrongPassword123!";
            string hashedPassword = _hasher.HashPassword(_testUser, password);

            // Act
            var result = _hasher.VerifyHashedPassword(_testUser, hashedPassword, wrongPassword);

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void HashPassword_GeneratesSecureHash()
        {
            // Arrange
            string password = "StrongPassword123!";

            // Act
            string hash = _hasher.HashPassword(_testUser, password);

            // Assert
            Assert.StartsWith("AQAAAA", hash); // Identity V3 prefix
        }
    }
}
