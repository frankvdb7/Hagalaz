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
            ArgumentNullException.ThrowIfNull(text);

            int byteCount = Encoding.UTF8.GetByteCount(text);
            Span<byte> textBytes = byteCount <= 1024 ? stackalloc byte[byteCount] : new byte[byteCount];
            Encoding.UTF8.GetBytes(text, textBytes);

            int hashLength = GetHashLength(hashType);
            if (hashLength == 0) return string.Empty;

            Span<byte> hash = stackalloc byte[hashLength];

            switch (hashType)
            {
                case HashType.MD5:
                    MD5.HashData(textBytes, hash);
                    break;
                case HashType.SHA1:
                    SHA1.HashData(textBytes, hash);
                    break;
                case HashType.SHA256:
                    SHA256.HashData(textBytes, hash);
                    break;
                case HashType.SHA384:
                    SHA384.HashData(textBytes, hash);
                    break;
                case HashType.SHA512:
                    SHA512.HashData(textBytes, hash);
                    break;
                default:
                    return string.Empty;
            }

            return Convert.ToHexStringLower(hash);
        }

        private static int GetHashLength(HashType hashType) => hashType switch
        {
            HashType.MD5 => 16,
            HashType.SHA1 => 20,
            HashType.SHA256 => 32,
            HashType.SHA384 => 48,
            HashType.SHA512 => 64,
            _ => 0
        };

        /// <summary>
        /// Creates a new instance of a hash algorithm based on the specified hash type.
        /// </summary>
        /// <param name="hashType">The type of hash algorithm to create.</param>
        /// <returns>A new instance of the specified <see cref="HashAlgorithm"/>, or <c>null</c> if the hash type is not supported.</returns>
        public static HashAlgorithm? CreateNewInstance(HashType hashType)
        {
            return hashType switch
            {
                HashType.MD5 => MD5.Create(),
                HashType.SHA1 => SHA1.Create(),
                HashType.SHA256 => SHA256.Create(),
                HashType.SHA384 => SHA384.Create(),
                HashType.SHA512 => SHA512.Create(),
                _ => null
            };
        }
    }
}
