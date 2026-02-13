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
        public void VerifyHashedPassword_WithPlaintext_ShouldReturnSuccessRehashNeeded()
        {
            var result = _hasher.VerifyHashedPassword(_user, "plaintext123", "plaintext123");
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithLegacySha256_ShouldReturnSuccessRehashNeeded()
        {
            var legacyHash = HashHelper.ComputeHash((_user.Email ?? string.Empty) + "password123", HashType.SHA256);

            var result = _hasher.VerifyHashedPassword(_user, legacyHash, "password123");
            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void HashPassword_ShouldUsePbkdf2()
        {
            var hash = _hasher.HashPassword(_user, "password123");

            // PBKDF2 hashes (V3) start with 'AQAAAA' in ASP.NET Core Identity
            Assert.StartsWith("AQAAAA", hash);

            var result = _hasher.VerifyHashedPassword(_user, hash, "password123");
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithWrongPassword_ShouldFail()
        {
            var legacyHash = HashHelper.ComputeHash(_user.Email + "password123", HashType.SHA256);

            var result = _hasher.VerifyHashedPassword(_user, legacyHash, "wrongpassword");
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }
}
