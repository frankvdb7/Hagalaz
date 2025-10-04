using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Abstractions.Logic.Codecs
{
    public interface ISectorCodec
    {
        ISector Decode(byte[] data, bool extended);
        byte[] Encode(ISector sector, byte[] dataBlock);
    }
}