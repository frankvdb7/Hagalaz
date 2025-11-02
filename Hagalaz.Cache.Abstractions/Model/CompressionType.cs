namespace Hagalaz.Cache.Abstractions.Model
{
    /// <summary>
    /// Defines the different types of compression used for data stored within the cache.
    /// </summary>
    public enum CompressionType : byte
    {
        /// <summary>
        /// Indicates that the data is not compressed.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the data is compressed using the BZIP2 algorithm.
        /// </summary>
        Bzip2 = 1,

        /// <summary>
        /// Indicates that the data is compressed using the GZIP algorithm.
        /// </summary>
        Gzip = 2
    }
}
