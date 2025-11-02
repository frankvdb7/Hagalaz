using System;
using System.IO;

namespace Hagalaz.Cache.Abstractions
{
    public interface IFileStore : IDisposable
    {
        int IndexFileCount { get; }
        int GetFileCount(int indexId);
        MemoryStream Read(int indexId, int fileId);
        void Write(int indexId, int fileId, MemoryStream data);
    }
}
