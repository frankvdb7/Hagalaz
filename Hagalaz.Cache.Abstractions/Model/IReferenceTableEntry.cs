using System.Collections.Generic;

namespace Hagalaz.Cache.Abstractions.Model
{
    public interface IReferenceTableEntry
    {
        /// <summary>
        /// Contains file crc32.
        /// </summary>
        int Crc32 { get; set; }

        /// <summary>
        /// Contains the version.
        /// </summary>
        int Version { get; set; }

        /// <summary>
        /// Contains file id.
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// Contains the maximum capacity for child entries.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Contains whirlpool digest of this file.
        /// </summary>
        byte[] WhirlpoolDigest { get; set; }

        /// <summary>
        /// Contains the entries.
        /// </summary>
        IEnumerable<KeyValuePair<int, IReferenceTableChildEntry>> Entries { get; }

        /// <summary>
        /// Initializes the entries.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        void InitializeEntries(int capacity);

        /// <summary>
        /// Gets the child entry with the specified id.
        /// </summary>
        /// <param name="childId">The child identifier.</param>
        /// <returns>The entry, or null if it does not exist.</returns>
        IReferenceTableChildEntry? GetEntry(int childId);

        /// <summary>
        /// Adds the entry.
        /// </summary>
        /// <param name="childId">The child identifier.</param>
        /// <param name="entry">The entry.</param>
        void AddEntry(int childId, IReferenceTableChildEntry entry);
    }
}