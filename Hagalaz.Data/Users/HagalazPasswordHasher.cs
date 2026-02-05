using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;

namespace Hagalaz.Data.Users
{
    public class HagalazPasswordHasher : PasswordHasher<Character>
    {
        public override string HashPassword(Character user, string password)
        {
            return base.HashPassword(user, password);
        }

        public override PasswordVerificationResult VerifyHashedPassword(Character user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // Identity V3 hashes start with 'AQAAAA'
            if (hashedPassword.StartsWith("AQAAAA"))
            {
                return base.VerifyHashedPassword(user, hashedPassword, providedPassword);
            }

            // Fallback to legacy SHA256 or plaintext
            if (hashedPassword == providedPassword)
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            var checkHash = HashHelper.ComputeHash(user.Email + providedPassword, HashType.SHA256);
            if (checkHash == hashedPassword)
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
