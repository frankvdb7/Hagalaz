using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Raido.Common.Protocol
{
    public interface IRaidoMessageDecoder
    {
        bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message);
    }
}