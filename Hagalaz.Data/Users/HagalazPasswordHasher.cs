using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;

namespace Hagalaz.Data.Users
{
    public class HagalazPasswordHasher : IPasswordHasher<Character>
    {
        private readonly PasswordHasher<Character> _identityPasswordHasher = new();

        /// <summary>
        /// Hashes the password using the standard ASP.NET Core Identity password hasher (PBKDF2).
        /// </summary>
        public string HashPassword(Character user, string password) => _identityPasswordHasher.HashPassword(user, password);

        /// <summary>
        /// Verifies a password against a hash. Supports both new standard Identity hashes and legacy SHA256 hashes.
        /// Plaintext passwords are no longer supported.
        /// </summary>
        public PasswordVerificationResult VerifyHashedPassword(Character user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // Check if it's a standard Identity hash (usually starts with 'AQAAAA' for V3)
            // Or if it looks like a standard base64 encoded Identity hash
            if (IsStandardIdentityHash(hashedPassword))
            {
                return _identityPasswordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            }

            // Fallback to old SHA256 hash (64 hex characters)
            var checkHash = HashHelper.ComputeHash(user.Email + providedPassword, HashType.SHA256);
            if (checkHash == hashedPassword)
            {
                // Return SuccessRehashNeeded to trigger an automatic upgrade to the new secure format on login
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }

        private static bool IsStandardIdentityHash(string hashedPassword)
        {
            // ASP.NET Core Identity V3 hashes are base64 encoded and start with 0x01 (AQAAAA in base64)
            // They are also typically longer than 64 characters (SHA256 hex)
            return hashedPassword.StartsWith("AQAAAA") || (hashedPassword.Length > 64 && IsBase64String(hashedPassword));
        }

        private static bool IsBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        }
    }
}
