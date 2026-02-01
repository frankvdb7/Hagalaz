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
            _user = new Character { Email = "test@example.com", UserName = "testuser" };
        }

        [Fact]
        public void HashPassword_ShouldReturnHash()
        {
            var password = "Password123!";
            var hash = _hasher.HashPassword(_user, password);

            Assert.NotNull(hash);
            Assert.NotEqual(password, hash);
        }

        [Fact]
        public void VerifyHashedPassword_LegacyHash_ShouldSucceedWithRehashNeeded()
        {
            var password = "Password123!";
            // Manual legacy hash: SHA256(email + password)
            var legacyHash = HashHelper.ComputeHash(_user.Email + password, HashType.SHA256);

            var result = _hasher.VerifyHashedPassword(_user, legacyHash, password);

            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
        }

        [Fact]
        public void VerifyHashedPassword_ModernHash_ShouldSucceed()
        {
            var password = "Password123!";
            var hash = _hasher.HashPassword(_user, password);

            var result = _hasher.VerifyHashedPassword(_user, hash, password);

            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void VerifyHashedPassword_WrongPassword_ShouldFail()
        {
            var password = "Password123!";
            var hash = _hasher.HashPassword(_user, password);

            var result = _hasher.VerifyHashedPassword(_user, hash, "WrongPassword");

            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void VerifyHashedPassword_Plaintext_ShouldFail()
        {
            var password = "Password123!";

            // This currently passes in the flawed implementation, but should fail after refactor
            var result = _hasher.VerifyHashedPassword(_user, password, password);

            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }
}
