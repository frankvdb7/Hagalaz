using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions;
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
            _logger.LogInformation("Opening cache file store path: {path}", Path.GetFullPath(rootPath));

            FileStream? dataFile = null;
            var indexFiles = new List<FileStream>();
            FileStream? mainIndexFile = null;

            try
            {
                // Open required files
                dataFile = OpenDataFile(rootPath);
                indexFiles = OpenIndexFiles(rootPath);
                mainIndexFile = OpenMainIndexFile(rootPath);

                ValidateIndexFiles(indexFiles, rootPath);

                // Initialize and return the file store
                return new FileStore(dataFile, indexFiles.ToArray(), mainIndexFile, _indexCodec, _sectorCodec);
            }
            catch
            {
                // Clean up resources if initialization fails
                dataFile?.Dispose();
                foreach (var indexFile in indexFiles)
                {
                    indexFile.Dispose();
                }

                mainIndexFile?.Dispose();
                throw;
            }
        }

        private static FileStream OpenDataFile(string rootPath) =>
            File.Open(Path.Combine(rootPath, "main_file_cache.dat2"),
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite);

        private static List<FileStream> OpenIndexFiles(string rootPath)
        {
            var indexFiles = new List<FileStream>();
            for (var i = 0;; i++)
            {
                var path = Path.Combine(rootPath, $"main_file_cache.idx{i}");
                if (!File.Exists(path)) break;

                indexFiles.Add(File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
            }

            return indexFiles;
        }

        private static FileStream OpenMainIndexFile(string rootPath) =>
            File.Open(Path.Combine(rootPath, "main_file_cache.idx255"),
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite);

        private static void ValidateIndexFiles(List<FileStream> indexFiles, string rootPath)
        {
            if (indexFiles.Count == 0)
            {
                throw new FileNotFoundException($"No cache found in directory: {rootPath}");
            }
        }
    }
}