using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Utilities;

namespace Hagalaz.Cache.Logic.Codecs
{
    /// <summary>
    /// Decodes a <see cref="Container"/> from a raw memory stream, handling different compression types and optional versioning.
    /// </summary>
    public class ContainerDecoder : IContainerDecoder
    {
        /// <summary>
        /// Decodes a data container from the provided memory stream.
        /// </summary>
        /// <param name="stream">The memory stream containing the raw, compressed container data.</param>
        /// <returns>A decoded <see cref="IContainer"/> with its data decompressed and version parsed.</returns>
        /// <exception cref="IOException">Thrown if the compression type is invalid or if the decompressed data length does not match the expected length.</exception>
        public IContainer Decode(MemoryStream stream)
        {
            /* decode the type and length */
            var type = (CompressionType)stream.ReadUnsignedByte();
            var compressedLength = stream.ReadInt();

            /* check if we should decompress the data or not */
            if (type == CompressionType.None)
            {
                /* simply grab the data and wrap it in a buffer */
                ValidatePayloadLength(stream, compressedLength);
                byte[] data = new byte[compressedLength];
                stream.ReadExactly(data);

                /* decode the version if present */
                short version = -1;
                if (stream.Remaining() >= 2)
                    version = stream.ReadShort();

                /* and return the decoded container */
                return new Container(type, new MemoryStream(data), version);
            }
            else
            {
                /* grab the length of the uncompressed data */
                var decompressedLength = stream.ReadInt();

                /* grab the data */
                ValidatePayloadLength(stream, compressedLength);
                var compressed = new byte[compressedLength];
                stream.ReadExactly(compressed);

                /* uncompress it */
                var decompressed = type switch
                {
                    CompressionType.Bzip2 => CompressionUtilities.BzipDecompress(compressed),
                    CompressionType.Gzip => CompressionUtilities.GzipDecompress(compressed),
                    _ => throw new IOException("Invalid compression type.")
                };

                /* check if the lengths are equal */
                if (decompressed.Length != decompressedLength)
                    throw new IOException("Decompressed length mismatch.");

                /* decode the version if present */
                short version = -1;
                if (stream.Remaining() >= 2)
                    version = stream.ReadShort();

                /* and return the decoded container */
                return new Container(type, new MemoryStream(decompressed), version);
            }
        }

        private static void ValidatePayloadLength(MemoryStream stream, int payloadLength)
        {
            if (payloadLength < 0 || payloadLength > stream.Remaining())
            {
                throw new InvalidDataException("Container payload length is invalid.");
            }
        }
    }
}
