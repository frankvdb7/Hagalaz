using System;
using System.Security.Cryptography;
using System.Text;

namespace Hagalaz.Security
{
    /// <summary>
    /// Provides method of hashing and comparing hashes.
    /// </summary>
    public static class HashHelper
    {
        /// <summary>
        /// Hashes a given text provided by the hash type.
        /// </summary>
        /// <param name="text">The text to encrpyt.</param>
        /// <param name="hashType">The hash type.</param>
        /// <returns>Returns a string.</returns>
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
