using System;
using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache
{
    public class IndexCodec : IIndexCodec
    {
        public Index Decode(ReadOnlySpan<byte> data)
        {
            if (data.Length < Index.IndexSize)
                throw new ArgumentException("Invalid input data", nameof(data));

            var position = 0;
            var size = ((data[position++] & 0xFF) << 16) + ((data[position++] & 0xFF) << 8) + (data[position++] & 0xFF);
            var sectorID = ((data[position++] & 0xFF) << 16) + ((data[position++] & 0xFF) << 8) + (data[position++] & 0xFF);

            return new Index(size, sectorID);
        }

        public byte[] Encode(Index index)
        {
            using (var writer = new MemoryStream(Index.IndexSize))
            {
                writer.WriteMedInt(index.Size);
                writer.WriteMedInt(index.SectorID);
                return writer.ToArray();
            }
        }
    }
}
