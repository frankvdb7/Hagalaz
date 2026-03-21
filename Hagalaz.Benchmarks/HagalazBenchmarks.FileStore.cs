using BenchmarkDotNet.Attributes;
using Hagalaz.Cache;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using System;
using System.IO;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    public partial class HagalazBenchmarks : IDisposable
    {
        private FileStore? _fileStore;
        private string? _tempDir;
        private byte[]? _smallData;
        private byte[]? _largeData;

        public void SetupFileStore()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            var dataFilePath = Path.Combine(_tempDir, "main_file_cache.dat2");
            var indexFilePath = Path.Combine(_tempDir, "main_file_cache.idx0");
            var mainIndexFilePath = Path.Combine(_tempDir, "main_file_cache.idx255");

            var dataFile = new FileStream(dataFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            var indexFile = new FileStream(indexFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            var mainIndexFile = new FileStream(mainIndexFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

            _fileStore = new FileStore(dataFile, new[] { indexFile }, mainIndexFile, new IndexCodec(), new SectorCodec());

            _smallData = new byte[100];
            new Random(42).NextBytes(_smallData);
            _largeData = new byte[10000];
            new Random(42).NextBytes(_largeData);

            _fileStore.Write(0, 1, new MemoryStream(_smallData));
            _fileStore.Write(0, 2, new MemoryStream(_largeData));
        }

        private void EnsureFileStoreInitialized()
        {
            if (_fileStore == null) SetupFileStore();
        }

        [Benchmark]
        public int FileStore_Read_Small()
        {
            EnsureFileStoreInitialized();
            using (var stream = _fileStore!.Read(0, 1))
            {
                return (int)stream.Length;
            }
        }

        [Benchmark]
        public int FileStore_Read_Large()
        {
            EnsureFileStoreInitialized();
            using (var stream = _fileStore!.Read(0, 2))
            {
                return (int)stream.Length;
            }
        }

        [Benchmark]
        public bool FileStore_Write_Small()
        {
            EnsureFileStoreInitialized();
            _fileStore!.Write(0, 3, new MemoryStream(_smallData!));
            return true;
        }

        [Benchmark]
        public bool FileStore_Write_Large()
        {
            EnsureFileStoreInitialized();
            _fileStore!.Write(0, 4, new MemoryStream(_largeData!));
            return true;
        }

        public void Dispose()
        {
            _fileStore?.Dispose();
            if (_tempDir != null && Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }
    }
}
