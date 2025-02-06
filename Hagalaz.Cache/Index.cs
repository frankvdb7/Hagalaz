using System;
using System.IO;
using Hagalaz.Cache.Extensions;

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

        /// <summary>
        /// Decodes the specified buffer.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static Index Decode(ReadOnlySpan<byte> data)
        {
            if (data.Length < IndexSize)
                throw new ArgumentException("Invalid input data", nameof(data));

            var position = 0;
            var size = ((data[position++] & 0xFF) << 16) + ((data[position++] & 0xFF) << 8) + (data[position++] & 0xFF);
            var sectorID = ((data[position++] & 0xFF) << 16) + ((data[position++] & 0xFF) << 8) + (data[position++] & 0xFF);

            return new Index(size, sectorID);
        }

        /// <summary>
        /// Encodes this instance.
        /// </summary>
        /// <returns></returns>
        public byte[] Encode()
        {
            using (var writer = new MemoryStream(IndexSize))
            {
                writer.WriteMedInt(Size);
                writer.WriteMedInt(SectorID);
                return writer.ToArray();
            }
        }
    }
}
