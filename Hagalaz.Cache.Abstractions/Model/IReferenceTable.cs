using System.Collections.Generic;

namespace Hagalaz.Cache.Abstractions.Model
{
    public interface IReferenceTable
    {
        int Version { get; set; }
        byte Protocol { get; }
        ReferenceTableFlags Flags { get; }
        /// <summary>Gets the number of archive groups present in this table.</summary>
        int Count { get; }
        int Capacity { get; }
        IEnumerable<KeyValuePair<int, IReferenceTableEntry>> Entries { get; }
        int GetFileId(string fileName);
        void AddEntry(int fileId, IReferenceTableEntry entry);
        IReferenceTableEntry? GetEntry(int fileId);
        IReferenceTableChildEntry? GetEntry(int fileId, int childId);
    }
}
