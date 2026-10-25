using System;
using System.Diagnostics;
using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Metrics;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Utilities;

namespace Hagalaz.Cache;

internal sealed class ArchiveSnapshotLoader(
    IFileStore store,
    IContainerDecoder containerDecoder,
    IArchiveDecoder archiveDecoder)
{
    public ArchiveSnapshot LoadSnapshot(ArchiveKey key, int memberCount)
    {
        var loadStart = Stopwatch.GetTimestamp();
        var loadSucceeded = false;
        try
        {
            using var container = ReadContainer(key.IndexId, key.ArchiveId);
            using var archive = archiveDecoder.Decode(container, memberCount);
            var snapshot = ArchiveSnapshot.Create(archive);
            loadSucceeded = true;
            return snapshot;
        }
        finally
        {
            CacheMetrics.RecordArchiveLoad(loadSucceeded, Stopwatch.GetElapsedTime(loadStart).TotalSeconds);
        }
    }

    public IContainer ReadContainer(int indexId, int fileId)
    {
        if (indexId != 255 && indexId >= store.IndexFileCount)
        {
            throw new FileNotFoundException();
        }

        if (indexId == 255 && fileId == 255)
        {
            throw new IOException("Checksum tables can only be read with the low level FileStore API!");
        }

        using var compressedFile = store.Read(indexId, fileId);
        return DecodeContainer(compressedFile);
    }

    public IContainer ReadContainer(int indexId, int fileId, int[] xteaKeys)
    {
        ArgumentNullException.ThrowIfNull(xteaKeys);
        if (xteaKeys.Length != 4)
        {
            throw new ArgumentException("Exactly four XTEA keys are required.", nameof(xteaKeys));
        }

        if (indexId == 255 && fileId == 255)
        {
            throw new IOException("Checksum tables can only be read with the low level FileStore API!");
        }

        using var compressedFile = store.Read(indexId, fileId);
        if (xteaKeys[0] == 0 && xteaKeys[1] == 0 && xteaKeys[2] == 0 && xteaKeys[3] == 0)
        {
            return DecodeContainer(compressedFile);
        }

        var buffer = compressedFile.ToArray();
        var decrypted = XteaDecryptor.DecryptXtea(xteaKeys, buffer, 5, buffer.Length);
        using var stream = new MemoryStream(decrypted);
        return DecodeContainer(stream);
    }

    private IContainer DecodeContainer(MemoryStream stream)
    {
        var decodeStart = Stopwatch.GetTimestamp();
        CompressionType? compression = null;
        var outcome = "failure";
        try
        {
            var container = containerDecoder.Decode(stream);
            compression = container.CompressionType;
            outcome = "success";
            return container;
        }
        finally
        {
            CacheMetrics.RecordContainerDecode(
                compression,
                outcome,
                Stopwatch.GetElapsedTime(decodeStart).TotalSeconds);
        }
    }
}
