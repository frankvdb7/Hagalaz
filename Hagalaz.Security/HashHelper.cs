using System;
using System.Security.Cryptography;
using System.Text;

namespace Hagalaz.Security
{
    /// <summary>
    /// Provides helper methods for computing hashes using various algorithms.
    /// </summary>
    public static class HashHelper
    {
        /// <summary>
        /// Computes the hash of a given string using the specified hash algorithm.
        /// </summary>
        /// <param name="text">The input string to hash.</param>
        /// <param name="hashType">The hash algorithm to use.</param>
        /// <returns>A hexadecimal string representation of the computed hash. Returns an empty string if the hash algorithm is not supported.</returns>
        public static string ComputeHash(string text, HashType hashType)
        {
            var algorithm = CreateNewInstance(hashType);
            if (algorithm == null)
            {
                return string.Empty;
            }

            using (algorithm)
            {
                var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(text));
                return string.Concat(Array.ConvertAll(hash, h => h.ToString("x2")));
            }
        }

        /// <summary>
        /// Creates a new instance of a hash algorithm based on the specified hash type.
        /// </summary>
        /// <param name="hashType">The type of hash algorithm to create.</param>
        /// <returns>A new instance of the specified <see cref="HashAlgorithm"/>, or <c>null</c> if the hash type is not supported.</returns>
        public static HashAlgorithm? CreateNewInstance(HashType hashType)
        {
            HashAlgorithm? result = null;
 
            switch (hashType)
            {
                case HashType.MD5:
                    result = MD5.Create();
                    break;
                case HashType.SHA1:
                    result = SHA1.Create();
                    break;
                case HashType.SHA256:
                    result = SHA256.Create();
                    break;
                case HashType.SHA384:
                    result = SHA384.Create();
                    break;
                case HashType.SHA512:
                    result = SHA512.Create();
                    break;
            }
 
            return result;
        }
    }
}
