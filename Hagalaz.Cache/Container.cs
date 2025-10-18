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
        /// Gets the type of compression used by this container.
        /// </summary>
        public CompressionType CompressionType { get; }

        /// <summary>
        /// Gets the decompressed data stored in this container.
        /// </summary>
        public MemoryStream Data { get; private set; }

        /// <summary>
        /// Gets or sets the version of the file within the container. A value of -1 indicates no version.
        /// </summary>
        public short Version { get; set; }

        /// <summary>
        /// Initializes a new instance of an unversioned and uncompressed <see cref="Container"/> class.
        /// </summary>
        /// <param name="data">The decompressed data to store in the container.</param>
        public Container(MemoryStream data)
            : this(CompressionType.None, data)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Container" /> class with a specified compression type and optional version.
        /// </summary>
        /// <param name="type">The compression type of the data.</param>
        /// <param name="data">The decompressed data to store in the container.</param>
        /// <param name="version">The version of the file. Defaults to -1 (unversioned).</param>
        public Container(CompressionType type, MemoryStream data, short version = -1)
        {
            CompressionType = type;
            Data = data;
            Version = version;
        }


        /// <summary>
        /// Determines whether this container includes version information.
        /// </summary>
        /// <returns><c>true</c> if the container is versioned; otherwise, <c>false</c>.</returns>
        public bool IsVersioned() => Version != -1;

        /// <summary>
        /// Removes the version information from this container by setting the version to -1.
        /// </summary>
        public void RemoveVersion() => Version = -1;

        /// <summary>
        /// Encodes the container's data into a byte array, applying the appropriate compression and prepending the necessary header.
        /// </summary>
        /// <param name="writeVersion">If set to <c>true</c>, the version number will be appended to the end of the encoded data if the container is versioned.</param>
        /// <returns>A byte array representing the fully encoded and compressed container.</returns>
        /// <exception cref="IOException">Thrown if an invalid compression type is specified.</exception>
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