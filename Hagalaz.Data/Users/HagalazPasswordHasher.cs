using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;

namespace Hagalaz.Data.Users
{
    public class HagalazPasswordHasher : IPasswordHasher<Character>
    {
        private readonly PasswordHasher<Character> _identityHasher = new();

        public string HashPassword(Character user, string password) => _identityHasher.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(Character user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // Standard Identity V3 prefix
            if (hashedPassword.StartsWith("AQAAAA"))
            {
                return _identityHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            }

            // Legacy plaintext check
            if (hashedPassword == providedPassword)
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            // Legacy SHA256 check
            var legacyHash = HashHelper.ComputeHash(user.Email + providedPassword, HashType.SHA256);
            if (legacyHash == hashedPassword)
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
