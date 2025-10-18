namespace Hagalaz.Cache
{
    /// <summary>
    /// Provides configuration options for the cache system.
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// The configuration key for the cache options section.
        /// </summary>
        public const string Key = "Hagalaz.Cache";
        
        /// <summary>
        /// Gets or sets the file system path to the game cache directory.
        /// </summary>
        public string Path { get; set; } = string.Empty;
    }
}
