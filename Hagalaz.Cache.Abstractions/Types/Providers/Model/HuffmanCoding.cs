namespace Hagalaz.Cache.Abstractions.Types.Providers.Model
{
    /// <summary>
    /// Represents the data structures required for Huffman encoding and decoding.
    /// This record encapsulates the tables necessary to compress and decompress text data.
    /// </summary>
    /// <param name="BitSizes">An array where each element represents the bit length of the Huffman code for a corresponding character.</param>
    /// <param name="Masks">An array of integer masks used during the decoding process to read the appropriate number of bits for each symbol.</param>
    /// <param name="DecryptKeys">An array representing the Huffman tree structure, used to map variable-length codes back to their original characters during decompression.</param>
    public record HuffmanCoding(byte[] BitSizes, int[] Masks, int[] DecryptKeys);
}