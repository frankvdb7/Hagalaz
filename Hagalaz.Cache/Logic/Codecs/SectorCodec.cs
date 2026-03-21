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
        private static readonly byte[] _zeroPadding = new byte[Sector.DataSize];

        public ISector Decode(ReadOnlySpan<byte> data, bool extended)
        {
            if (data.Length < (extended ? Sector.ExtendedDataHeaderSize : Sector.DataHeaderSize))
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

        public byte[] Encode(ISector sector, byte[] dataBlock) => Encode(sector, dataBlock.AsSpan());

        public byte[] Encode(ISector sector, ReadOnlySpan<byte> dataBlock)
        {
            var array = new byte[Sector.DataSize];
            Encode(sector, dataBlock, array);
            return array;
        }

        public void Encode(ISector sector, ReadOnlySpan<byte> dataBlock, Span<byte> destination)
        {
            var headerSize = sector.FileID > ushort.MaxValue ? Sector.ExtendedDataHeaderSize : Sector.DataHeaderSize;
            if (destination.Length < headerSize + dataBlock.Length)
                throw new ArgumentException("Destination span too small.");

            int pos = 0;
            if (sector.FileID > ushort.MaxValue)
            {
                destination[pos++] = (byte)(sector.FileID >> 24);
                destination[pos++] = (byte)(sector.FileID >> 16);
                destination[pos++] = (byte)(sector.FileID >> 8);
                destination[pos++] = (byte)sector.FileID;
            }
            else
            {
                destination[pos++] = (byte)(sector.FileID >> 8);
                destination[pos++] = (byte)sector.FileID;
            }

            destination[pos++] = (byte)(sector.ChunkID >> 8);
            destination[pos++] = (byte)sector.ChunkID;

            destination[pos++] = (byte)(sector.NextSectorID >> 16);
            destination[pos++] = (byte)(sector.NextSectorID >> 8);
            destination[pos++] = (byte)sector.NextSectorID;

            destination[pos++] = (byte)sector.IndexID;

            dataBlock.CopyTo(destination.Slice(pos));

            // pad remainder if necessary
            int totalWritten = pos + dataBlock.Length;
            if (totalWritten < Sector.DataSize && destination.Length >= Sector.DataSize)
            {
                destination.Slice(totalWritten, Sector.DataSize - totalWritten).Clear();
            }
        }
    }
}