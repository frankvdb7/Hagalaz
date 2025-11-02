using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Models
{
    /// <summary>
    /// A <see cref="Sector"/> contains a header and data. The header contains information
    /// used to verify the integrity of the cache like the current file id, type and
    /// chunk. It also contains a pointer to the next sector such that the sectors
    /// form a singly-linked list. The data is simply up to 512 or 510 bytes of the file.
    /// </summary>
    public struct Sector : ISector
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
    }
}