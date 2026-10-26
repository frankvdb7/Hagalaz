using Hagalaz.Cache.Abstractions;

namespace Hagalaz.TestSupport;

internal sealed class MemoryFileStore : IFileStore
{
    private readonly object _sync = new();
    private readonly Dictionary<(int IndexId, int FileId), byte[]> _files = [];
    private readonly Dictionary<(int IndexId, int FileId), int> _readCounts = [];

    public int IndexFileCount => 255;

    public int GetFileCount(int indexId)
    {
        lock (_sync)
        {
            return _files.Keys.Where(key => key.IndexId == indexId).Select(key => key.FileId).DefaultIfEmpty(-1).Max() + 1;
        }
    }

    public MemoryStream Read(int indexId, int fileId)
    {
        lock (_sync)
        {
            var key = (indexId, fileId);
            if (!_files.TryGetValue(key, out var data))
            {
                throw new FileNotFoundException($"Cache file {indexId}:{fileId} was not found.");
            }

            _readCounts[key] = _readCounts.GetValueOrDefault(key) + 1;
            return new MemoryStream(data, writable: false);
        }
    }

    public void Write(int indexId, int fileId, MemoryStream data)
    {
        lock (_sync)
        {
            _files[(indexId, fileId)] = data.ToArray();
        }
    }

    public void Set(int indexId, int fileId, byte[] data)
    {
        using var stream = new MemoryStream(data);
        Write(indexId, fileId, stream);
    }

    public byte[] GetBytes(int indexId, int fileId)
    {
        lock (_sync)
        {
            return _files[(indexId, fileId)].ToArray();
        }
    }

    public int GetReadCount(int indexId, int fileId)
    {
        lock (_sync)
        {
            return _readCounts.GetValueOrDefault((indexId, fileId));
        }
    }

    public void Dispose()
    {
    }
}
