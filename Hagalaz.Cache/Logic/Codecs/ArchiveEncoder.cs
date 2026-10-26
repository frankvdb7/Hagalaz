using System;
using System.Buffers.Binary;
using System.IO;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Logic.Codecs
{
    /// <summary>
    /// Encodes decoded archive members using the single-chunk archive format.
    /// </summary>
    internal sealed class ArchiveEncoder
    {
        internal MemoryStream Encode(IArchive archive)
        {
            ArgumentNullException.ThrowIfNull(archive);

            var entries = archive.Entries ?? throw new InvalidDataException("Archive entries are not available.");
            if (entries.Length == 0)
            {
                throw new InvalidDataException("An archive must contain at least one member.");
            }

            if (entries.Length == 1)
            {
                return new MemoryStream(GetEntryData(entries[0]));
            }

            using var output = new MemoryStream();
            foreach (var entry in entries)
            {
                output.Write(GetEntryData(entry));
            }

            Span<byte> lengthBuffer = stackalloc byte[sizeof(int)];
            var previousMemberLength = 0;
            foreach (var entry in entries)
            {
                var memberLength = checked((int)entry.Length);
                BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, memberLength - previousMemberLength);
                output.Write(lengthBuffer);
                previousMemberLength = memberLength;
            }

            output.WriteByte(1);
            return new MemoryStream(output.ToArray());
        }

        private static byte[] GetEntryData(MemoryStream? entry)
        {
            return entry?.ToArray() ?? throw new InvalidDataException("Archive contains a missing member stream.");
        }
    }
}
