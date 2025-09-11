using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache
{
    /// <summary>
    /// A <see cref="ReferenceTable"/> holds details for all the files with a single
    /// type, such as checksums, versions and archive members. There are also
    /// optional fields for identifier hashes and whirlpool digests.
    /// </summary>
    public class ReferenceTable : IReferenceTable
    {
        /// <summary>
        /// The entries
        /// </summary>
        private readonly SortedList<int, ReferenceTableEntry> _entries;

        /// <summary>
        /// Contains revision of this information table.
        /// </summary>
        /// <value>The revision.</value>
        public int Version { get; set; }
        /// <summary>
        /// Contains the protocol.
        /// </summary>
        public byte Protocol { get; }
        /// <summary>
        /// Contains the file store flags.
        /// </summary>
        public ReferenceTableFlags Flags { get; }
        /// <summary>
        /// Gets the maximum number of files in this table.
        /// </summary>
        /// <value>
        /// The maximum number of files.
        /// </value>
        public int Capacity => _entries.Capacity;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceTable"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="version">The version.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="fileCount">The file count.</param>
        public ReferenceTable(byte protocol, int version, ReferenceTableFlags flags, int fileCount)
        {
            Protocol = protocol;
            Version = version;
            Flags = flags;
            _entries = new SortedList<int, ReferenceTableEntry>(fileCount);
        }

        /// <summary>
        /// Encodes this <see cref="ReferenceTable"/> into a <see cref="MemoryStream"/>.
        /// </summary>
        /// <returns></returns>
        public MemoryStream Encode()
        {
            /* we can't (easily) predict the size ahead of time, so we write to a
               stream and then to the buffer */
            var ew = new MemoryStream();

            /* write the header */
            ew.WriteByte(Protocol);
            if (Protocol >= 6)
                ew.WriteInt(Version);
            ew.WriteByte((byte)Flags);

            /* calculate and write the number of non-null entries */
            ew.WriteBigSmart(Capacity);

            /* write the ids */
            int last = 0;
            for (var id = 0; id < Capacity; id++)
            {
                if (!_entries.ContainsKey(id))
                {
                    continue;
                }

                int delta = id - last;
                ew.WriteBigSmart(delta);
                last = id;
            }

            /* write the identifiers if required */
            if (Flags.HasFlag(ReferenceTableFlags.Identifiers))
                foreach (var entry in _entries.Values)
                    ew.WriteInt(entry.Id);

            /* write the CRC checksums */
            foreach (var entry in _entries.Values)
                ew.WriteInt(entry.Crc32);

            /* write the whirlpool digests if required */
            if (Flags.HasFlag(ReferenceTableFlags.Digests))
                foreach (var entry in _entries.Values)
                    ew.WriteBytes(entry.WhirlpoolDigest);

            /* write the versions */
            foreach (var entry in _entries.Values)
                ew.WriteInt(entry.Version);

            /* calculate and write the number of non-null child entries */
            foreach (var entry in _entries.Values)
                ew.WriteBigSmart(entry.Capacity);

            /* write the child ids */
            foreach (var entry in _entries.Values)
            {
                last = 0;
                for (var id = 0; id < entry.Capacity; id++)
                {
                    if (!entry.ContainsEntry(id))
                    {
                        continue;
                    }

                    int delta = id - last;
                    ew.WriteBigSmart(delta);
                    last = id;
                }
            }

            /* write the child identifiers if required  */
            if (Flags.HasFlag(ReferenceTableFlags.Identifiers))
                foreach (var entry in _entries.Values)
                    foreach (var child in entry.Entries)
                        ew.WriteInt(child.Id);

            /* flip the buffer and return the stream */
            ew.Flip();
            return ew;
        }

        /// <summary>
        /// Find's file id by given name.
        /// </summary>
        /// <param name="fileName">The name.</param>
        /// <returns>System.Int32.</returns>
        public int GetFileId(string fileName)
        {
            var hash = CalculateId(fileName);
            for (var id = 0; id <= Capacity; id++)
            {
                if (!_entries.ContainsKey(id))
                {
                    continue;
                }
                if (_entries[id].Id == hash)
                {
                    return id;
                }
            }
            return -1;
        }

        /// <summary>
        /// Adds the entry.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="entry">The entry.</param>
        public void AddEntry(int fileId, ReferenceTableEntry entry) => _entries.Add(fileId, entry);

        /// <summary>
        /// Gets the entry with the specified id, or null if it does not
        /// exist.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        /// <returns>The entry.</returns>
        public ReferenceTableEntry? GetEntry(int fileId)
        {
            _entries.TryGetValue(fileId, out var entry);
            return entry;
        }

        /// <summary>
        /// Gets the child entry.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="childId">The child identifier.</param>
        /// <returns></returns>
        public ReferenceTableChildEntry? GetEntry(int fileId, int childId)
        {
            var entry = GetEntry(fileId);
            return entry?.GetEntry(childId);
        }

        /// <summary>
        /// Calculate's name hash.
        /// </summary>
        /// <param name="name">Name of the file to calculate hash.</param>
        /// <returns>Return's calculated namehash.</returns>
        public static int CalculateId(string name)
        {
            var id = 0;
            foreach (var character in name)
            {
                id = ((byte)character) + ((id << 5) - id);
            }
            return id;
        }
    }
}
