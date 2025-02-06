using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;

namespace Hagalaz.Data.Users
{
    public class HagalazPasswordHasher : IPasswordHasher<Character>
    {
        public string HashPassword(Character user, string password) => HashHelper.ComputeHash(user.Email + password, HashType.SHA256);

        public PasswordVerificationResult VerifyHashedPassword(Character user, string hashedPassword, string providedPassword)
        {
            if (hashedPassword == providedPassword)
            {
                return PasswordVerificationResult.Success;
            }

            var checkHash = HashHelper.ComputeHash(user.Email + providedPassword, HashType.SHA256);
            return checkHash == hashedPassword ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
        }
    }
}
