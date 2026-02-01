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
            if (string.IsNullOrWhiteSpace(hashedPassword) || providedPassword == null)
            {
                return PasswordVerificationResult.Failed;
            }

            // Check if it's an Identity V3 hash (starts with 'AQAAAA' - base64 of 0x01)
            // Identity V3 hashes are PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
            // They are encoded as [versionByte][keyDerivationPrf][iterCount][saltSize][salt][subkey]
            if (hashedPassword.StartsWith("AQAAAA"))
            {
                return _identityHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            }

            // Legacy SHA256(Email + password) support
            // We return SuccessRehashNeeded to signal that the password should be rehashed using the modern algorithm.
            var checkHash = HashHelper.ComputeHash(user.Email + providedPassword, HashType.SHA256);
            if (checkHash == hashedPassword)
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
