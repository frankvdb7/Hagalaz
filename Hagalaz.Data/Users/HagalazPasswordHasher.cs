using System;
using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;

namespace Hagalaz.Data.Users
{
    public class HagalazPasswordHasher : IPasswordHasher<Character>
    {
        private readonly PasswordHasher<Character> _baseHasher = new PasswordHasher<Character>();

        public string HashPassword(Character user, string password) => _baseHasher.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(Character user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // ASP.NET Core Identity V3 hashes start with 'AQAAAA'
            if (hashedPassword.StartsWith("AQAAAA"))
            {
                return _baseHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            }

            // Legacy SHA256(Email + password) check
            var checkHash = HashHelper.ComputeHash(user.Email + providedPassword, HashType.SHA256);
            if (string.Equals(checkHash, hashedPassword, StringComparison.OrdinalIgnoreCase))
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            // Fallback for legacy plaintext (if any)
            if (hashedPassword == providedPassword)
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
