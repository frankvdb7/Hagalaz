using System.IO;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    public interface INpcTypeCodec
    {
        INpcType Decode(int id, MemoryStream stream);

        MemoryStream Encode(INpcType npcType);
    }
}