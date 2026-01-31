using Xunit;
using Hagalaz.Data.Users;
using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;

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
        public void VerifyHashedPassword_WithPlaintext_ReturnsFailed()
        {
            // Plaintext should no longer work
            var result = _hasher.VerifyHashedPassword(_user, "plaintext123", "plaintext123");
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithSha256_ReturnsSuccessRehashNeeded()
        {
            // Old SHA256 hashes should still work but indicate rehash is needed
            var password = "password123";
            var hashedPassword = HashHelper.ComputeHash(_user.Email + password, HashType.SHA256);

            var result = _hasher.VerifyHashedPassword(_user, hashedPassword, password);
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void HashPassword_UsesNewSecureFormat()
        {
            var password = "password123";
            var hash = _hasher.HashPassword(_user, password);

            // Should be a standard Identity V3 hash (base64, starts with AQAAAA)
            Assert.StartsWith("AQAAAA", hash);

            // Verifying the new hash should return Success
            var result = _hasher.VerifyHashedPassword(_user, hash, password);
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithInvalidPassword_ReturnsFailed()
        {
            var password = "password123";
            var hash = _hasher.HashPassword(_user, password);

            var result = _hasher.VerifyHashedPassword(_user, hash, "wrongpassword");
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }
}
