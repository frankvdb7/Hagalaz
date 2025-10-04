using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Cache
{
    public class FileStoreLoader : IFileStoreLoader
    {
        private readonly ILogger<FileStore> _logger;
        private readonly IIndexCodec _indexCodec;
        private readonly ISectorCodec _sectorCodec;

        public FileStoreLoader(ILogger<FileStore> logger, IIndexCodec indexCodec, ISectorCodec sectorCodec)
        {
            _logger = logger;
            _indexCodec = indexCodec;
            _sectorCodec = sectorCodec;
        }

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
