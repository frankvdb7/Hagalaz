using System.IO;

namespace Hagalaz.Cache
{
    public interface IReferenceTableFactory
    {
        IReferenceTable Decode(MemoryStream stream, bool isNamed);
    }
}
