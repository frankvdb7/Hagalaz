using System.Buffers;

namespace Hagalaz.Cache.Abstractions.Types
{
    public interface IHuffmanEncoder
    {
        public void Encode(string text, IBufferWriter<byte> output);
    }
}