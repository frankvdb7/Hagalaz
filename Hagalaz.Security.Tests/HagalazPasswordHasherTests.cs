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
        private readonly Character _user = new() { Email = "test@example.com" };

        [Fact]
        public void HashPassword_ShouldReturnV3Hash()
        {
            var password = "StrongPassword123!";
            var hash = _hasher.HashPassword(_user, password);

            Assert.StartsWith("AQAAAA", hash);
        }

        [Fact]
        public void VerifyHashedPassword_WithValidV3Hash_ShouldReturnSuccess()
        {
            var password = "StrongPassword123!";
            var hash = _hasher.HashPassword(_user, password);

            var result = _hasher.VerifyHashedPassword(_user, hash, password);

            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithLegacySHA256Hash_ShouldReturnSuccessRehashNeeded()
        {
            var password = "LegacyPassword";
            var legacyHash = HashHelper.ComputeHash(_user.Email + password, HashType.SHA256);

            var result = _hasher.VerifyHashedPassword(_user, legacyHash, password);

            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithPlaintext_ShouldReturnSuccessRehashNeeded()
        {
            var password = "PlaintextPassword";

            var result = _hasher.VerifyHashedPassword(_user, password, password);

            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_WithWrongPassword_ShouldReturnFailed()
        {
            var password = "StrongPassword123!";
            var hash = _hasher.HashPassword(_user, password);

            var result = _hasher.VerifyHashedPassword(_user, hash, "WrongPassword");

            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }
}
