using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;

namespace Hagalaz.Cache.Utilities
{
    /// <summary>
    /// Untilities for compression/decompression.
    /// </summary>
    public static class CompressionUtilities
    {
        /// <summary>
        /// Gzips the compress.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static byte[] GzipCompress(byte[] data)
        {
            byte[] output;
            using (MemoryStream io = new MemoryStream())
            {
                using (GZipOutputStream gzip = new GZipOutputStream(io))
                {
                    gzip.Write(data, 0, data.Length);
                }
                output = io.ToArray();
            }
            return output;
        }

        /// <summary>
        /// Unzips gziped file.
        /// </summary>
        /// <param name="data">File bytes to unzip.</param>
        /// <returns>The unzipped byte data.</returns>
        public static byte[] GzipDecompress(byte[] data)
        {
            byte[] output;
            using (MemoryStream io = new MemoryStream())
            {
                using (GZipInputStream gzip = new GZipInputStream(new MemoryStream(data)))
                {
                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        int read = gzip.Read(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            break;
                        }
                        io.Write(buffer, 0, read);
                    }
                    output = io.ToArray();
                }
            }
            return output;
        }

        /// <summary>
        /// Bzips the compress.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static byte[] BzipCompress(byte[] data)
        {
            byte[] output;
            using (MemoryStream io = new MemoryStream())
            {
                using (BZip2OutputStream gzip = new BZip2OutputStream(io))
                {
                    gzip.Write(data, 0, data.Length);
                }
                output = io.ToArray();
            }
            return output;
        }

        /// <summary>
        /// Decompresses a byte array using BZIP2.
        /// </summary>
        /// <param name="data">The compressed data.</param>
        /// <returns>The uncompressed data.</returns>
        public static byte[] BzipDecompress(byte[] data)
        {
            byte[] output;
            byte[] newData = new byte[data.Length + 4];
            System.Array.Copy(data, 0, newData, 4, data.Length);
            newData[0] = (byte)'B';
            newData[1] = (byte)'Z';
            newData[2] = (byte)'h';
            newData[3] = (byte)'1';

            using (var io = new MemoryStream())
            {
                using (var bzip = new BZip2InputStream(new MemoryStream(newData)))
                {
                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        int read = bzip.Read(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            break;
                        }
                        io.Write(buffer, 0, read);
                    }
                    output = io.ToArray();
                }
            }
            return output;
        }
    }
}
