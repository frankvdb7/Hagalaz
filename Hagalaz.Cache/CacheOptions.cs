namespace Hagalaz.Cache
{
    /// <summary>
    /// 
    /// </summary>
    public class CacheOptions
    {
        public const string Key = "Hagalaz.Cache";
        
        /// <summary>
        /// Gets or sets the cache path.
        /// </summary>
        /// <value>
        /// The cache path.
        /// </value>
        public string Path { get; set; } = string.Empty;
    }
}
