using Hagalaz.Data.Entities;
using Hagalaz.Data.Users;
using Hagalaz.Security;
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
            _user = new Character { Email = "test@example.com", UserName = "testuser" };
        }

        [Fact]
        public void HashPassword_ReturnsV3Hash()
        {
            // Act
            var hash = _hasher.HashPassword(_user, "Password123!");

            // Assert
            Assert.StartsWith("AQAAAA", hash); // ASP.NET Core Identity V3 prefix
        }

        [Fact]
        public void VerifyHashedPassword_V3Hash_Success()
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
        public void VerifyHashedPassword_LegacySHA256Hash_SuccessRehashNeeded()
        {
            // Arrange
            var password = "LegacyPassword123!";
            var legacyHash = HashHelper.ComputeHash(_user.Email + password, HashType.SHA256);

            // Act
            var result = _hasher.VerifyHashedPassword(_user, legacyHash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_LegacySHA256Hash_CaseInsensitive_SuccessRehashNeeded()
        {
            // Arrange
            var password = "LegacyPassword123!";
            var legacyHash = HashHelper.ComputeHash(_user.Email + password, HashType.SHA256).ToUpper();

            // Act
            var result = _hasher.VerifyHashedPassword(_user, legacyHash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_Plaintext_SuccessRehashNeeded()
        {
            // Arrange
            var password = "PlaintextPassword";

            // Act
            var result = _hasher.VerifyHashedPassword(_user, password, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_IncorrectPassword_Failed()
        {
            // Arrange
            var password = "Password123!";
            var hash = _hasher.HashPassword(_user, password);

            // Act
            var result = _hasher.VerifyHashedPassword(_user, hash, "WrongPassword");

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void VerifyHashedPassword_NullOrEmptyStoredHash_Failed()
        {
            // Act & Assert
            Assert.Equal(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(_user, null!, "password"));
            Assert.Equal(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(_user, "", "password"));
            Assert.Equal(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(_user, "   ", "password"));
        }
    }
}
