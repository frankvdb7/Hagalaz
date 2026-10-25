using System;
using System.IO;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache;

internal sealed class ReferenceTableCache
{
    private readonly IReferenceTableProvider _provider;
    private readonly IReferenceTable?[] _tables;
    private readonly object[] _indexLocks;

    public ReferenceTableCache(IReferenceTableProvider provider, int indexCount)
    {
        _provider = provider;
        _tables = new IReferenceTable?[indexCount];
        _indexLocks = new object[indexCount];
        for (var indexId = 0; indexId < _indexLocks.Length; indexId++)
        {
            _indexLocks[indexId] = new object();
        }
    }

    public IReferenceTable Get(int indexId)
    {
        if (indexId >= _tables.Length)
        {
            throw new FileNotFoundException();
        }

        if (indexId < 0)
        {
            throw new IndexOutOfRangeException();
        }

        var table = Volatile.Read(ref _tables[indexId]);
        if (table is not null)
        {
            return table;
        }

        lock (_indexLocks[indexId])
        {
            table = Volatile.Read(ref _tables[indexId]);
            if (table is null)
            {
                table = _provider.ReadReferenceTable(indexId);
                Volatile.Write(ref _tables[indexId], table);
            }

            return table;
        }
    }

    /// <summary>
    /// Clears a memoized table. Callers must coordinate this under CacheApi's exclusive writer lock.
    /// </summary>
    public void Clear(int indexId)
    {
        if (indexId >= 0 && indexId < _tables.Length && indexId != 255)
        {
            Volatile.Write(ref _tables[indexId], null);
        }
    }
}
