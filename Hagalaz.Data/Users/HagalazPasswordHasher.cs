using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;
using System;

namespace Hagalaz.Data.Users
{
    public class HagalazPasswordHasher : IPasswordHasher<Character>
    {
        private readonly PasswordHasher<Character> _modernHasher = new();

        public string HashPassword(Character user, string password) => _modernHasher.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(Character user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // If it starts with AQAAAA, it's likely a modern Identity hash
            if (hashedPassword.StartsWith("AQAAAA"))
            {
                try
                {
                    return _modernHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
                }
                catch (FormatException)
                {
                    // Fallback if it wasn't actually a valid base64 identity hash
                }
            }

            // Fallback for legacy SHA256(Email + password) hashes and plaintext
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
