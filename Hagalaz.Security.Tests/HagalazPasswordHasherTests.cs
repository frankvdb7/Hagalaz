using Hagalaz.Data.Entities;
using Hagalaz.Data.Users;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class HagalazPasswordHasherTests
    {
        private readonly HagalazPasswordHasher _hasher;
        private readonly Character _user;

        public HagalazPasswordHasherTests()
        {
            _hasher = new HagalazPasswordHasher();
            _user = new Character { Email = "test@example.com" };
        }

        [Fact]
        public void HashPassword_ShouldReturnModernIdentityHash()
        {
            // Arrange
            var password = "password123";

            // Act
            var hash = _hasher.HashPassword(_user, password);

            // Assert
            Assert.StartsWith("AQAAAA", hash); // Identity V3 prefix
        }

        [Fact]
        public void VerifyHashedPassword_ShouldSucceedWithModernHash()
        {
            // Arrange
            var password = "password123";
            var hash = _hasher.HashPassword(_user, password);

            // Act
            var result = _hasher.VerifyHashedPassword(_user, hash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_ShouldSucceedWithRehashNeeded_Sha256()
        {
            // Arrange
            var password = "password123";
            var hash = HashHelper.ComputeHash(_user.Email + password, HashType.SHA256);

            // Act
            var result = _hasher.VerifyHashedPassword(_user, hash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_ShouldSucceedWithRehashNeeded_Plaintext()
        {
            // Arrange
            var password = "password123";

            // Act
            var result = _hasher.VerifyHashedPassword(_user, password, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_ShouldFailWithIncorrectPassword_Modern()
        {
            // Arrange
            var password = "password123";
            var wrongPassword = "wrongpassword";
            var hash = _hasher.HashPassword(_user, password);

            // Act
            var result = _hasher.VerifyHashedPassword(_user, hash, wrongPassword);

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void VerifyHashedPassword_ShouldFailWithIncorrectPassword_Sha256()
        {
            // Arrange
            var password = "password123";
            var wrongPassword = "wrongpassword";
            var hash = HashHelper.ComputeHash(_user.Email + password, HashType.SHA256);

            // Act
            var result = _hasher.VerifyHashedPassword(_user, hash, wrongPassword);

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }
}
