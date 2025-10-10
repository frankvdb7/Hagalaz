using Hagalaz.Cache.Abstractions.Providers.Model;

namespace Hagalaz.Cache.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that supplies Huffman coding tables.
    /// Huffman coding is used for lossless data compression, and this interface abstracts
    /// the source of the compression data (e.g., from a file, a network resource, or hardcoded).
    /// </summary>
    public interface IHuffmanCodeProvider
    {
        /// <summary>
        /// Retrieves the Huffman coding data, including bit sizes, masks, and decryption keys.
        /// </summary>
        /// <returns>A <see cref="HuffmanCoding"/> record containing the necessary data for Huffman compression and decompression.</returns>
        public HuffmanCoding GetCoding();
    }
}