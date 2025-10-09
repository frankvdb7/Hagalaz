using System.Buffers;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines a contract for a Huffman encoder, which is responsible for compressing
    /// text data using Huffman coding.
    /// </summary>
    public interface IHuffmanEncoder
    {
        /// <summary>
        /// Encodes a string of text using Huffman coding and writes the compressed
        /// data to the specified buffer writer.
        /// </summary>
        /// <param name="text">The string of text to be encoded.</param>
        /// <param name="output">The buffer writer to which the compressed byte data will be written.</param>
        public void Encode(string text, IBufferWriter<byte> output);
    }
}