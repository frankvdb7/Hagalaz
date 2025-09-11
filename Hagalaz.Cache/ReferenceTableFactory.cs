using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache
{
    public class ReferenceTableFactory : IReferenceTableFactory
    {
        public IReferenceTable Decode(MemoryStream stream, bool readEntries)
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
                table.AddEntry(id, new ReferenceTableEntry(index++));

            /* read the identifiers if present */
            if (table.Flags.HasFlag(ReferenceTableFlags.Identifiers))
            {
                foreach (var id in ids)
                    table.GetEntry(id).Id = stream.ReadInt();
            }

            /* read the CRC32 checksums */
            foreach (var id in ids)
                table.GetEntry(id).Crc32 = stream.ReadInt();

            /* read the whirlpool digests if present */
            if (table.Flags.HasFlag(ReferenceTableFlags.Digests))
            {
                foreach (var id in ids)
                {
                    byte[] data = new byte[64];
                    stream.Read(data, 0, 64);
                    table.GetEntry(id).WhirlpoolDigest = data;
                }
            }

            /* read the version numbers */
            foreach (var id in ids)
                table.GetEntry(id).Version = stream.ReadInt();

            /* read the subfile sizes */
            int[][] members = new int[size][];
            foreach (var id in ids)
            {
                members[id] = new int[table.Protocol >= 7 ? stream.ReadBigSmart() : (stream.ReadShort() & 0xFFFF)];
                table.GetEntry(id).InitializeEntries(members[id].Length);
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
                    table.GetEntry(id).AddEntry(childId, new ReferenceTableChildEntry(index++));
                }
            }

            /* read the child identifiers if present */
            if (table.Flags.HasFlag(ReferenceTableFlags.Identifiers))
            {
                foreach (var id in ids)
                {
                    foreach (var child in members[id])
                    {
                        var entry = table.GetEntry(id).GetEntry(child);
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
    }
}
