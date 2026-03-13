using System;
using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;

namespace Hagalaz.Data.Users
{
    public class HagalazPasswordHasher : IPasswordHasher<Character>
    {
        private readonly PasswordHasher<Character> _baseHasher = new();

        public string HashPassword(Character user, string password) => _baseHasher.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(Character user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // Identity V3 prefix is 'AQAAAA'
            if (hashedPassword.StartsWith("AQAAAA", StringComparison.Ordinal))
            {
                return _baseHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            }

            // Check legacy SHA256 (64 hex chars)
            if (hashedPassword.Length == 64)
            {
                var checkHash = HashHelper.ComputeHash(user.Email + providedPassword, HashType.SHA256);
                if (string.Equals(checkHash, hashedPassword, StringComparison.OrdinalIgnoreCase))
                {
                    return PasswordVerificationResult.SuccessRehashNeeded;
                }
            }

            // Check legacy plaintext
            if (string.Equals(hashedPassword, providedPassword, StringComparison.Ordinal))
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
