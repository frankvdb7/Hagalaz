using System.IO;

namespace Hagalaz.Cache
{
    public interface IReferenceTableDecoder
    {
        IReferenceTable Decode(MemoryStream stream, bool readEntries);
    }
}
