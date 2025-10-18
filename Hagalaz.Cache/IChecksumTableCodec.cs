using System.IO;
using System.Numerics;

namespace Hagalaz.Cache
{
    /// <summary>
    /// Defines the contract for a codec that handles the encoding and decoding of a <see cref="ChecksumTable"/>.
    /// </summary>
    public interface IChecksumTableCodec
    {
        /// <summary>
        /// Encodes a <see cref="ChecksumTable"/> into a memory stream.
        /// </summary>
        /// <param name="table">The checksum table to encode.</param>
        /// <returns>A memory stream containing the encoded checksum table.</returns>
        MemoryStream Encode(ChecksumTable table);

        /// <summary>
        /// Encodes a <see cref="ChecksumTable"/> into a memory stream, with an option to include whirlpool digests.
        /// </summary>
        /// <param name="table">The checksum table to encode.</param>
        /// <param name="whirlpool">A flag indicating whether to include whirlpool digests for each entry.</param>
        /// <returns>A memory stream containing the encoded checksum table.</returns>
        MemoryStream Encode(ChecksumTable table, bool whirlpool);

        /// <summary>
        /// Encodes a <see cref="ChecksumTable"/> into a memory stream, with options for whirlpool digests and RSA encryption.
        /// </summary>
        /// <param name="table">The checksum table to encode.</param>
        /// <param name="whirlpool">A flag indicating whether to include whirlpool digests.</param>
        /// <param name="modulus">The RSA modulus for encryption.</param>
        /// <param name="privateKey">The RSA private key for encryption.</param>
        /// <returns>A memory stream containing the encoded and potentially encrypted checksum table.</returns>
        MemoryStream Encode(ChecksumTable table, bool whirlpool, BigInteger modulus, BigInteger privateKey);

        /// <summary>
        /// Decodes a <see cref="ChecksumTable"/> from a memory stream.
        /// </summary>
        /// <param name="stream">The memory stream containing the encoded checksum table.</param>
        /// <returns>The decoded <see cref="ChecksumTable"/>.</returns>
        ChecksumTable Decode(MemoryStream stream);

        /// <summary>
        /// Decodes a <see cref="ChecksumTable"/> from a memory stream, with an option for whirlpool digests.
        /// </summary>
        /// <param name="stream">The memory stream containing the encoded checksum table.</param>
        /// <param name="whirlpool">A flag indicating whether the stream contains whirlpool digests.</param>
        /// <returns>The decoded <see cref="ChecksumTable"/>.</returns>
        ChecksumTable Decode(MemoryStream stream, bool whirlpool);

        /// <summary>
        /// Decodes a <see cref="ChecksumTable"/> from a memory stream, with options for whirlpool digests and RSA decryption.
        /// </summary>
        /// <param name="stream">The memory stream containing the encoded checksum table.</param>
        /// <param name="whirlpool">A flag indicating whether the stream contains whirlpool digests.</param>
        /// <param name="modulus">The RSA modulus for decryption.</param>
        /// <param name="publicKey">The RSA public key for decryption.</param>
        /// <returns>The decoded and potentially decrypted <see cref="ChecksumTable"/>.</returns>
        ChecksumTable Decode(MemoryStream stream, bool whirlpool, BigInteger modulus, BigInteger publicKey);
    }
}
