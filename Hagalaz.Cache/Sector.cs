using System;
using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache
{
    /// <summary>
    /// A <see cref="Sector"/> contains a header and data. The header contains information
    /// used to verify the integrity of the cache like the current file id, type and
    /// chunk. It also contains a pointer to the next sector such that the sectors
    /// form a singly-linked list. The data is simply up to 512 or 510 bytes of the file.
    /// </summary>
    public struct Sector
    {
        /// <summary>
        /// The size of a data block in the data file.
        /// </summary>
        public const int DataBlockSize = 512;
        /// <summary>
        /// The size of a header block in the data file.
        /// </summary>
        public const int DataHeaderSize = 8;
        /// <summary>
        /// The extended size of a data block in the data fileId.
        /// </summary>
        public const int ExtendedDataBlockSize = 510;
        /// <summary>
        /// The extended size of a header block in the data fileId.
        /// </summary>
        public const int ExtendedDataHeaderSize = 10;
        /// <summary>
        /// The overall size of a block in the data file.
        /// </summary>
        public const int DataSize = DataBlockSize + DataHeaderSize;
        /// <summary>
        /// The maximum file size of a single file.
        /// </summary>
        public const int MaxFileSize = 1000000;

        /// <summary>
        /// The type of file this sector contains.
        /// </summary>
        public int IndexID { get; }
        /// <summary>
        /// The id of the file this sector contains.
        /// </summary>
        public int FileID { get; }
        /// <summary>
        /// The chunk within the file that this sector contains.
        /// </summary>
        public int ChunkID { get; }
        /// <summary>
        /// The next sector.
        /// </summary>
        public int NextSectorID { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sector" /> class.
        /// </summary>
        /// <param name="indexID">The type.</param>
        /// <param name="fileID">The identifier.</param>
        /// <param name="chunkID">The chunk.</param>
        /// <param name="nextSectorID">The next sector.</param>
        public Sector(int fileID, int chunkID, int nextSectorID, int indexID)
        {
            IndexID = indexID;
            FileID = fileID;
            ChunkID = chunkID;
            NextSectorID = nextSectorID;
        }

        /// <summary>
        /// Decodes the specified buffer.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="extended">if set to <c>true</c> [extended].</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static Sector Decode(byte[] data, bool extended)
        {
            if (data.Length != DataSize)
                throw new ArgumentException();

            int position = 0;
            int fileID;
            if (extended) fileID = ((data[position++] & 0xFF) << 24) | ((data[position++] & 0xFF) << 16) | ((data[position++] & 0xFF) << 8) | (data[position++] & 0xFF);
            else fileID = ((data[position++] & 0xFF) << 8) | (data[position++] & 0xFF) & 0xFFFF;
            int chunkID = ((data[position++] & 0xFF) << 8) | (data[position++] & 0xFF) & 0xFFFF;
            int nextSectorID = ((data[position++] & 0xFF) << 16) | ((data[position++] & 0xFF) << 8) | (data[position++] & 0xFF);
            byte cacheID = (byte)(data[position] & 0xFF);

            return new Sector(fileID, chunkID, nextSectorID, cacheID);
        }

        /// <summary>
        /// Encodes this instance.
        /// </summary>
        /// <param name="dataBlock">The data block.</param>
        /// <returns></returns>
        public byte[] Encode(byte[] dataBlock)
        {
            using (var writer = new MemoryStream(DataSize))
            {
                if (FileID > ushort.MaxValue)
                    writer.WriteInt(FileID);
                else
                    writer.WriteShort(FileID);
                writer.WriteShort(ChunkID);
                writer.WriteMedInt(NextSectorID);
                writer.WriteByte(IndexID);
                writer.WriteBytes(dataBlock);
                return writer.ToArray();
            }
        }
    }
}
