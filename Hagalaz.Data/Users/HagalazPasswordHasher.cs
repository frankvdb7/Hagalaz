using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;
using System;

namespace Hagalaz.Data.Users
{
    /// <summary>
    /// A password hasher that supports standard ASP.NET Core Identity PBKDF2 hashing
    /// and provides a migration path for legacy SHA256 and plaintext passwords.
    /// </summary>
    public class HagalazPasswordHasher : IPasswordHasher<Character>
    {
        private readonly PasswordHasher<Character> _standardHasher = new();

        public string HashPassword(Character user, string password) => _standardHasher.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(Character user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // Try standard PBKDF2 verification first
            var result = _standardHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            if (result != PasswordVerificationResult.Failed)
            {
                return result;
            }

            // Fallback for legacy SHA256(Email + password) hashes
            var checkHash = HashHelper.ComputeHash((user.Email ?? string.Empty) + providedPassword, HashType.SHA256);
            if (string.Equals(checkHash, hashedPassword, StringComparison.OrdinalIgnoreCase))
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            // Fallback for legacy plaintext passwords
            if (string.Equals(hashedPassword, providedPassword, StringComparison.Ordinal))
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
