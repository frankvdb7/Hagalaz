using ICSharpCode.SharpZipLib.Checksum;

namespace Hagalaz.Cache.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public static class BufferUtilities
    {
        /// <summary>
        /// Gets the CRC checksum.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public static int GetCrcChecksum(byte[] data, int offset, int count)
        {
            var crc = new Crc32();
            for (int i = 0; i < count; i++)
            {
                crc.Update(data[i]);
            }
            return (int)crc.Value;
        }
    }
}
