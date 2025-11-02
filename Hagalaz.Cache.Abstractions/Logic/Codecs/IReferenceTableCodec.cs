using System.IO;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Abstractions.Logic.Codecs
{
    public interface IReferenceTableCodec
    {
        IReferenceTable Decode(MemoryStream stream);

        IReferenceTable Decode(MemoryStream stream, bool readEntries);

        MemoryStream Encode(IReferenceTable table);
    }
}
