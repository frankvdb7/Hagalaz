using System;

namespace Hagalaz.Cache
{
    /// <summary>
    /// An <see cref="Index"/> points to a file inside a <see cref="FileStore"/>.
    /// </summary>
    public struct Index
    {
        /// <summary>
        /// The size of an index, in bytes.
        /// </summary>
        public const int IndexSize = 6;

        /// <summary>
        /// Contains the size of the file in bytes.
        /// </summary>
        public int Size { get; }
        /// <summary>
        /// Contains the number of the first sector that contains the file.
        /// </summary>
        public int SectorID { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Index" /> class.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="sectorID">The sector.</param>
        public Index(int size, int sectorID)
        {
            Size = size;
            SectorID = sectorID;
        }

    }
}
