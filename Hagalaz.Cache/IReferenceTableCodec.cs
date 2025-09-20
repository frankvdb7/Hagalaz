using System.IO;

namespace Hagalaz.Cache
{
    public interface IReferenceTableCodec
    {
        IReferenceTable Decode(MemoryStream stream);

        IReferenceTable Decode(MemoryStream stream, bool readEntries);

        MemoryStream Encode(IReferenceTable table);
    }
}
