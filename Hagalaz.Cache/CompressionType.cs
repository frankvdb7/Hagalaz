namespace Hagalaz.Cache
{
    /// <summary>
    /// 
    /// </summary>
    public enum CompressionType : byte
    {
        /// <summary>
        /// This type indicates that no compression is used.
        /// </summary>
        None = 0,
        /// <summary>
        /// This type indicates that BZIP2 compression is used.
        /// </summary>
        Bzip2 = 1,
        /// <summary>
        /// This type indicates that GZIP compression is used.
        /// </summary>
        Gzip = 2
    }
}
