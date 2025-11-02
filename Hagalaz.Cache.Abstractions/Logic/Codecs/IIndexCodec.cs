using System;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Abstractions.Logic.Codecs
{
    public interface IIndexCodec
    {
        IIndex Decode(ReadOnlySpan<byte> data);
        byte[] Encode(IIndex index);
    }
}
