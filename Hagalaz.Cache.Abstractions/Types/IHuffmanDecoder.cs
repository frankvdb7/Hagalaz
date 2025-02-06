using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Cache.Abstractions.Types
{
    public interface IHuffmanDecoder
    {
        public bool TryDecode(in ReadOnlySequence<byte> input, int length, [NotNullWhen(true)] out string? value);
    }
}