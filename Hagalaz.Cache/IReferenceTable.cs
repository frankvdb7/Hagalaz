using System.IO;

namespace Hagalaz.Cache
{
    public interface IReferenceTable
    {
        int Version { get; set; }
        byte Protocol { get; }
        ReferenceTableFlags Flags { get; }
        int Capacity { get; }
        MemoryStream Encode();
        int GetFileId(string fileName);
        void AddEntry(int fileId, ReferenceTableEntry entry);
        ReferenceTableEntry? GetEntry(int fileId);
        ReferenceTableChildEntry? GetEntry(int fileId, int childId);
    }
}
