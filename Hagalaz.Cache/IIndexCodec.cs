using System;

namespace Hagalaz.Cache
{
    public interface IIndexCodec
    {
        Index Decode(ReadOnlySpan<byte> data);
        byte[] Encode(Index index);
    }
}
