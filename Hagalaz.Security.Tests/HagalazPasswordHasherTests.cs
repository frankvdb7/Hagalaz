using Xunit;
using Hagalaz.Data.Entities;
using Hagalaz.Data.Users;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;

namespace Hagalaz.Security.Tests
{
    public class HagalazPasswordHasherTests
    {
        private readonly HagalazPasswordHasher _hasher = new();
        private readonly Character _testUser = new() { Email = "test@example.com" };

        [Fact]
        public void HashPassword_ShouldReturnV3Hash()
        {
            var password = "StrongPassword123!";
            var hash = _hasher.HashPassword(_testUser, password);

            Assert.StartsWith("AQAAAA", hash);
        }

        [Fact]
        public void VerifyHashedPassword_ShouldVerifyV3Hash()
        {
            var password = "StrongPassword123!";
            var hash = _hasher.HashPassword(_testUser, password);

            var result = _hasher.VerifyHashedPassword(_testUser, hash, password);

            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_ShouldVerifyLegacySha256AndRequestRehash()
        {
            var password = "LegacyPassword123";
            var legacyHash = HashHelper.ComputeHash(_testUser.Email + password, HashType.SHA256);

            var result = _hasher.VerifyHashedPassword(_testUser, legacyHash, password);

            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_ShouldVerifyPlaintextAndRequestRehash()
        {
            var password = "PlaintextPassword";

            var result = _hasher.VerifyHashedPassword(_testUser, password, password);

            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_ShouldFailForIncorrectPassword()
        {
            var password = "StrongPassword123!";
            var hash = _hasher.HashPassword(_testUser, password);

            var result = _hasher.VerifyHashedPassword(_testUser, hash, "WrongPassword");

            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void VerifyHashedPassword_ShouldFailForNullOrEmptyHash()
        {
            var result = _hasher.VerifyHashedPassword(_testUser, "", "SomePassword");
            Assert.Equal(PasswordVerificationResult.Failed, result);

            result = _hasher.VerifyHashedPassword(_testUser, null!, "SomePassword");
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }
}
