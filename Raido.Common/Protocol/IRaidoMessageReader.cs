using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Raido.Common.Protocol
{
    public interface IRaidoMessageReader<TMessage>
    {
        bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out TMessage? message);
    }
}