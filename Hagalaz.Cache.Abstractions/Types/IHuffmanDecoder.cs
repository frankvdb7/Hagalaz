using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines a contract for a Huffman decoder, which is responsible for decompressing
    /// data that has been compressed using Huffman coding.
    /// </summary>
    public interface IHuffmanDecoder
    {
        /// <summary>
        /// Attempts to decode a sequence of bytes containing Huffman-compressed data into a string.
        /// </summary>
        /// <param name="input">A read-only sequence of bytes representing the compressed data.</param>
        /// <param name="length">The number of characters to decode from the input sequence.</param>
        /// <param name="value">When this method returns, contains the decoded string if the operation was successful, or <c>null</c> otherwise.</param>
        /// <returns><c>true</c> if the decoding was successful; otherwise, <c>false</c>.</returns>
        public bool TryDecode(in ReadOnlySequence<byte> input, int length, [NotNullWhen(true)] out string? value);
    }
}