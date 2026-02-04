using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Hagalaz.Security;

namespace Hagalaz.Data.Users
{
    /// <summary>
    /// A custom password hasher for the Hagalaz application that provides modern PBKDF2 hashing
    /// while maintaining backward compatibility with legacy SHA256 and plaintext passwords.
    /// </summary>
    public class HagalazPasswordHasher : PasswordHasher<Character>
    {
        /// <summary>
        /// Verifies a hashed password against a provided password.
        /// </summary>
        /// <param name="user">The user whose password is being verified.</param>
        /// <param name="hashedPassword">The stored password hash.</param>
        /// <param name="providedPassword">The password provided for verification.</param>
        /// <returns>A <see cref="PasswordVerificationResult"/> indicating the result of the verification.</returns>
        public override PasswordVerificationResult VerifyHashedPassword(Character user, string hashedPassword, string providedPassword)
        {
            // Identity V3 hashes are prefixed with 'AQAAAA'
            if (hashedPassword.StartsWith("AQAAAA"))
            {
                return base.VerifyHashedPassword(user, hashedPassword, providedPassword);
            }

            // Legacy SHA256(Email + password)
            var checkHash = HashHelper.ComputeHash(user.Email + providedPassword, HashType.SHA256);
            if (checkHash == hashedPassword)
            {
                // Successful verification, but needs to be rehashed using the modern algorithm.
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            // Legacy Plaintext fallback
            if (hashedPassword == providedPassword)
            {
                // Successful verification, but needs to be rehashed using the modern algorithm.
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
