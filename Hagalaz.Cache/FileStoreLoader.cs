using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Cache
{
    /// <summary>
    /// Handles the loading and initialization of a <see cref="FileStore"/> from a specified directory.
    /// It locates and opens the necessary data and index files (`main_file_cache.dat2`, `main_file_cache.idx*`).
    /// </summary>
    public class FileStoreLoader : IFileStoreLoader
    {
        private readonly ILogger<FileStore> _logger;
        private readonly IIndexCodec _indexCodec;
        private readonly ISectorCodec _sectorCodec;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStoreLoader"/> class.
        /// </summary>
        public FileStoreLoader(ILogger<FileStore> logger, IIndexCodec indexCodec, ISectorCodec sectorCodec)
        {
            _logger = logger;
            _indexCodec = indexCodec;
            _sectorCodec = sectorCodec;
        }

        /// <summary>
        /// Opens the cache files from the specified root path and initializes a new <see cref="IFileStore"/>.
        /// </summary>
        /// <param name="rootPath">The directory path containing the cache files.</param>
        /// <returns>An initialized <see cref="IFileStore"/> ready for use.</returns>
        /// <exception cref="FileNotFoundException">Thrown if no cache index files are found in the specified directory.</exception>
        public IFileStore Open(string rootPath)
        {
            _logger.LogInformation("Cache file store path: {0}", Path.GetFullPath(rootPath));

            /* open the main data file stream */
            var dataFile = File.Open(rootPath + @"/main_file_cache.dat2", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            /* open all the index file streams */
            var indexFiles = new List<FileStream>();
            for (int i = 0; ; i++)
            {
                var path = rootPath + @"/main_file_cache.idx" + i;
                if (File.Exists(path))
                    indexFiles.Add(File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
                else
                    break;
            }

            if (indexFiles.Count == 0) throw new FileNotFoundException("No cache found in directory: " + rootPath);

            /* open the main index file stream */
            var mainIndexFile = File.Open(rootPath + @"/main_file_cache.idx255", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            /* initialize the store */
            var store = new FileStore(dataFile, indexFiles.ToArray(), mainIndexFile, _indexCodec, _sectorCodec);

            return store;
        }
    }
}
