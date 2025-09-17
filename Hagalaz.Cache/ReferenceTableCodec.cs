using Hagalaz.Cache.Extensions;
using System.IO;

namespace Hagalaz.Cache
{
    public class ReferenceTableCodec : IReferenceTableCodec
    {
        public IReferenceTable Decode(MemoryStream stream)
        {
            return Decode(stream, true);
        }

        public IReferenceTable Decode(MemoryStream stream, bool readEntries)
        {
            /* read header */
            byte protocol = (byte)(stream.ReadSignedByte() & 0xFF);
            if (protocol < 5 || protocol > 7)
                throw new InvalidDataException("Invalid reference table protocol!");
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
                {
                    var entry = table.GetEntry(id) ?? throw new InvalidDataException($"Corrupt reference table: entry {id} not found while reading identifiers.");
                    entry.Id = stream.ReadInt();
                }
            }

            /* read the CRC32 checksums */
            foreach (var id in ids)
            {
                var entry = table.GetEntry(id) ?? throw new InvalidDataException($"Corrupt reference table: entry {id} not found while reading checksums.");
                entry.Crc32 = stream.ReadInt();
            }

            /* read the whirlpool digests if present */
            if (table.Flags.HasFlag(ReferenceTableFlags.Digests))
            {
                foreach (var id in ids)
                {
                    var entry = table.GetEntry(id) ?? throw new InvalidDataException($"Corrupt reference table: entry {id} not found while reading digests.");
                    byte[] data = new byte[64];
                    stream.Read(data, 0, 64);
                    entry.WhirlpoolDigest = data;
                }
            }

            /* read the version numbers */
            foreach (var id in ids)
            {
                var entry = table.GetEntry(id) ?? throw new InvalidDataException($"Corrupt reference table: entry {id} not found while reading versions.");
                entry.Version = stream.ReadInt();
            }

            /* read the subfile sizes */
            int[][] members = new int[size][];
            foreach (var id in ids)
            {
                members[id] = new int[table.Protocol >= 7 ? stream.ReadBigSmart() : (stream.ReadShort() & 0xFFFF)];
                var entry = table.GetEntry(id) ?? throw new InvalidDataException($"Corrupt reference table: entry {id} not found while reading subfile sizes.");
                entry.InitializeEntries(members[id].Length);
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
                var parentEntry = table.GetEntry(id) ?? throw new InvalidDataException($"Corrupt reference table: entry {id} not found while allocating child entries.");
                foreach (var childId in members[id])
                {
                    parentEntry.AddEntry(childId, new ReferenceTableChildEntry(index++));
                }
            }

            /* read the child identifiers if present */
            if (table.Flags.HasFlag(ReferenceTableFlags.Identifiers))
            {
                foreach (var id in ids)
                {
                    foreach (var child in members[id])
                    {
                        var parentEntry = table.GetEntry(id) ?? throw new InvalidDataException($"Corrupt reference table: entry {id} not found while reading child identifiers.");
                        var entry = parentEntry.GetEntry(child) ?? throw new System.InvalidOperationException($"{nameof(ReferenceTableChildEntry)} with id {child} does not exist in {nameof(ReferenceTableEntry)} with id {id}");
                        entry.Id = stream.ReadInt();
                    }
                }
            }
            return table;
        }

        public MemoryStream Encode(IReferenceTable table)
        {
            /* we can't (easily) predict the size ahead of time, so we write to a
               stream and then to the buffer */
            var ew = new MemoryStream();

            /* write the header */
            ew.WriteByte(table.Protocol);
            if (table.Protocol >= 6)
                ew.WriteInt(table.Version);
            ew.WriteByte((byte)table.Flags);

            /* calculate and write the number of non-null entries */
            ew.WriteBigSmart(table.Capacity);

            /* write the ids */
            var refTable = (table as ReferenceTable) ?? new ReferenceTable(table);
            int last = 0;
            foreach (var entry in refTable.Entries)
            {
                int delta = entry.Index - last;
                ew.WriteBigSmart(delta);
                last = entry.Index;
            }

            /* write the identifiers if required */
            if (table.Flags.HasFlag(ReferenceTableFlags.Identifiers))
                foreach (var entry in refTable.Entries)
                    ew.WriteInt(entry.Id);

            /* write the CRC checksums */
            foreach (var entry in refTable.Entries)
                ew.WriteInt(entry.Crc32);

            /* write the whirlpool digests if required */
            if (table.Flags.HasFlag(ReferenceTableFlags.Digests))
                foreach (var entry in refTable.Entries)
                    ew.WriteBytes(entry.WhirlpoolDigest);

            /* write the versions */
            foreach (var entry in refTable.Entries)
                ew.WriteInt(entry.Version);

            /* calculate and write the number of non-null child entries */
            foreach (var entry in refTable.Entries)
                ew.WriteBigSmart(entry.Capacity);

            /* write the child ids */
            foreach (var entry in refTable.Entries)
            {
                last = 0;
                foreach (var child in entry.Entries)
                {
                    int delta = child.Index - last;
                    ew.WriteBigSmart(delta);
                    last = child.Index;
                }
            }

            /* write the child identifiers if required  */
            if (table.Flags.HasFlag(ReferenceTableFlags.Identifiers))
                foreach (var entry in refTable.Entries)
                    foreach (var child in entry.Entries)
                        ew.WriteInt(child.Id);


            /* flip the buffer and return the stream */
            ew.Flip();
            return ew;
        }
    }
}
