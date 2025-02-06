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
    public class ReferenceTable
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
        private ReferenceTable(byte protocol, int version, ReferenceTableFlags flags, int fileCount)
        {
            Protocol = protocol;
            Version = version;
            Flags = flags;
            _entries = new SortedList<int, ReferenceTableEntry>(fileCount);
        }

        /// <summary>
        /// Decodes the slave checksum table contained in the specified
        /// buffer.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="readEntries">Whether to decode and read the table entries.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Invalid information file version!</exception>
        public static ReferenceTable Decode(MemoryStream stream, bool readEntries)
        {
            /* read header */
            byte protocol = (byte)(stream.ReadSignedByte() & 0xFF);
            if (protocol < 5 || protocol > 7)
                throw new Exception("Invalid reference table protocol!");
            int version = protocol >= 6 ? stream.ReadInt() : 0;
            var flags = (ReferenceTableFlags)stream.ReadUnsignedByte();
            int fileCount = protocol >= 7 ? stream.ReadBigSmart() : (stream.ReadShort() & 0xFFFF);

            /* create a new table */
            var table = new ReferenceTable(protocol, version, flags, fileCount);

            /* return the table if we do not need to read the entries */
            if (!readEntries)
            {
                return table;
            }
            
            /* read the ids */
            int[] ids = new int[table.Capacity];
            int accumulator = 0, size = -1;
            for (var i = 0; i < table.Capacity; i++)
            {
                int delta = table.Protocol >= 7 ? stream.ReadBigSmart() : (stream.ReadShort() & 0xFFFF);
                ids[i] = accumulator += delta;

                if (ids[i] > size)
                    size = ids[i];
            }
            size++;

            /* and allocate specific entries within that array */
            int index = 0;
            foreach (var id in ids)
                table._entries.Add(id, new ReferenceTableEntry(index++));

            /* read the identifiers if present */
            if (table.Flags.HasFlag(ReferenceTableFlags.Identifiers))
            {
                foreach (var id in ids)
                    table._entries[id].Id = stream.ReadInt();
            }

            /* read the CRC32 checksums */
            foreach (var id in ids)
                table._entries[id].Crc32 = stream.ReadInt();

            /* read the whirlpool digests if present */
            if (table.Flags.HasFlag(ReferenceTableFlags.Digests))
            {
                foreach (var id in ids)
                {
                    byte[] data = new byte[64];
                    stream.Read(data, 0, 64);
                    table._entries[id].WhirlpoolDigest = data;
                }
            }

            /* read the version numbers */
            foreach (var id in ids)
                table._entries[id].Version = stream.ReadInt();

            /* read the subfile sizes */
            int[][] members = new int[size][];
            foreach (var id in ids)
            {
                members[id] = new int[table.Protocol >= 7 ? stream.ReadBigSmart() : (stream.ReadShort() & 0xFFFF)];
                table._entries[id].InitializeEntries(members[id].Length);
            }

            /* read the child ids */
            foreach (var id in ids)
            {
                /* reset the accumulator and size */
                accumulator = 0;
                size = -1;

                /* loop through the array of ids */
                for (var i = 0; i < members[id].Length; i++)
                {
                    int delta = (table.Protocol >= 7 ? stream.ReadBigSmart() : (stream.ReadShort() & 0xFFFF));
                    members[id][i] = accumulator += delta;

                    if (members[id][i] > size)
                        size = members[id][i];
                }
                size++;


                
                /* and allocate specific entries within the array */
                index = 0;
                foreach (int childId in members[id])
                {
                    table._entries[id].AddEntry(childId, new ReferenceTableChildEntry(index++));
                }
            }

            /* read the child identifiers if present */
            if (table.Flags.HasFlag(ReferenceTableFlags.Identifiers))
            {
                foreach (var id in ids)
                {
                    foreach (var child in members[id])
                    {
                        var entry = table._entries[id].GetEntry(child);
                        if (entry == null)
                        {
                            throw new InvalidOperationException($"{nameof(ReferenceTableChildEntry)} does not exist in {nameof(ReferenceTableEntry)}");
                        }
                        entry.Id = stream.ReadInt();
                    }
                }
            }
            return table;
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
