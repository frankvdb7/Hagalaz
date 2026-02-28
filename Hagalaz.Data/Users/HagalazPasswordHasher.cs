using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;

namespace Hagalaz.Data.Users
{
    public class HagalazPasswordHasher : IPasswordHasher<Character>
    {
        private readonly PasswordHasher<Character> _passwordHasher = new();

        public string HashPassword(Character user, string password) => _passwordHasher.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(Character user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // If it's a new hash (ASP.NET Core Identity Version 3 starts with AQAAAA)
            if (hashedPassword.StartsWith("AQAAAA"))
            {
                return _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            }

            // Fallback to legacy SHA256(Email + password)
            var legacyHash = HashHelper.ComputeHash(user.Email + providedPassword, HashType.SHA256);
            if (legacyHash == hashedPassword)
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            // Fallback to plaintext (legacy)
            if (hashedPassword == providedPassword)
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
