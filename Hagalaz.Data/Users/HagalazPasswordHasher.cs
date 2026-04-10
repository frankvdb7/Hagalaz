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
            if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            try
            {
                var result = _baseHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
                if (result != PasswordVerificationResult.Failed)
                {
                    return result;
                }
            }
            catch (System.FormatException)
            {
                // Fallback to legacy check below
            }

            // Fallback for legacy SHA256 hashes
            var checkHash = HashHelper.ComputeHash(user.Email + providedPassword, HashType.SHA256);
            if (checkHash == hashedPassword)
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
