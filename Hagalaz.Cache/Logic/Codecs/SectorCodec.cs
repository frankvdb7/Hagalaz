using System;
using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Models;

namespace Hagalaz.Cache.Logic.Codecs
{
    public class SectorCodec : ISectorCodec
    {
        public ISector Decode(byte[] data, bool extended)
        {
            if (data.Length != Sector.DataSize)
                throw new ArgumentException();

            int position = 0;
            int fileID;
            if (extended)
                fileID = ((data[position++] & 0xFF) << 24) | ((data[position++] & 0xFF) << 16) | ((data[position++] & 0xFF) << 8) | (data[position++] & 0xFF);
            else
                fileID = ((data[position++] & 0xFF) << 8) | (data[position++] & 0xFF) & 0xFFFF;

            int chunkID = ((data[position++] & 0xFF) << 8) | (data[position++] & 0xFF) & 0xFFFF;
            int nextSectorID = ((data[position++] & 0xFF) << 16) | ((data[position++] & 0xFF) << 8) | (data[position++] & 0xFF);
            byte cacheID = (byte)(data[position] & 0xFF);

            return new Sector(fileID, chunkID, nextSectorID, cacheID);
        }

        public byte[] Encode(ISector sector, byte[] dataBlock)
        {
            using (var writer = new MemoryStream(Sector.DataSize))
            {
                if (sector.FileID > ushort.MaxValue)
                    writer.WriteInt(sector.FileID);
                else
                    writer.WriteShort(sector.FileID);
                writer.WriteShort(sector.ChunkID);
                writer.WriteMedInt(sector.NextSectorID);
                writer.WriteByte(sector.IndexID);
                writer.WriteBytes(dataBlock);
                return writer.ToArray();
            }
        }
    }
}