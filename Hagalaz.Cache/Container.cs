using System;
using System.IO;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Utilities;

namespace Hagalaz.Cache
{
    /// <summary>
    /// A <see cref="Container"/> holds an optionally compressed file. This class can be
    /// used to decompress and compress containers. A container can also have a two
    /// byte trailer which specifies the version of the file within it.
    /// </summary>
    public class Container : IContainer
    {
        /// <summary>
        /// The type of compression this container uses.
        /// </summary>
        public CompressionType CompressionType { get; }

        /// <summary>
        /// The decompressed data.
        /// </summary>
        public MemoryStream Data { get; private set; }

        /// <summary>
        /// Contains the version.
        /// </summary>
        public short Version { get; set; }

        /// <summary>
        /// Creates a new unversioned and uncompressed <see cref="Container"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public Container(MemoryStream data)
            : this(CompressionType.None, data)
        {
        }

        /// <summary>
        /// Creates a new versioned <see cref="Container" />.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        /// <param name="version">The version.</param>
        public Container(CompressionType type, MemoryStream data, short version = -1)
        {
            CompressionType = type;
            Data = data;
            Version = version;
        }


        /// <summary>
        /// Determines whether this instance is versioned.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is versioned; otherwise, <c>false</c>.
        /// </returns>
        public bool IsVersioned() => Version != -1;

        /// <summary>
        /// Removes the version.
        /// </summary>
        public void RemoveVersion() => Version = -1;

        /// <summary>
        /// Encodes this instance.
        /// </summary>
        /// <param name="writeVersion">if set to <c>true</c> [write version].</param>
        /// <returns></returns>
        /// <exception cref="System.IO.IOException">Invalid compression type.</exception>
        public byte[] Encode(bool writeVersion = true)
        {
            /* grab the data as a byte array for compression */
            byte[] bytes = Data.ToArray();

            /* compress the data */
            byte[] compressed = CompressionType switch
            {
                CompressionType.None => bytes,
                CompressionType.Gzip => CompressionUtilities.GzipCompress(bytes),
                CompressionType.Bzip2 => CompressionUtilities.BzipCompress(bytes),
                _ => throw new IOException("Invalid compression type.")
            };

            /* calculate the size of the header and trailer and allocate a buffer */
            int header = 5 + (CompressionType == CompressionType.None ? 0 : 4) + ((IsVersioned() && writeVersion) ? 2 : 0);
            using (var buffer = new MemoryStream(header + compressed.Length))
            {
                buffer.WriteByte((byte)CompressionType);
                buffer.WriteInt(compressed.Length);
                if (CompressionType != CompressionType.None)
                    buffer.WriteInt(bytes.Length);

                /* write the compressed bytes */
                buffer.WriteBytes(compressed);

                /* write the trailer with the optional version */
                if (IsVersioned() && writeVersion)
                    buffer.WriteShort(Version);

                /* flip the buffer and return it */
                return buffer.ToArray();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Data.Dispose();
            Data = null!;
        }
    }
}