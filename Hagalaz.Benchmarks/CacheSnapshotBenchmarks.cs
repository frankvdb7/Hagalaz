using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Hagalaz.Cache;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;
using Hagalaz.Cache.Types.Providers;
using Microsoft.Extensions.Options;

namespace Hagalaz.Benchmarks;

[MemoryDiagnoser]
public class CacheSnapshotMemberReadBenchmarks
{
    private CacheBenchmarkFixture _fixture = null!;
    private CacheApi _archiveReconstructionCache = null!;
    private CacheApi _coldSnapshotCache = null!;
    private CacheApi _warmSnapshotCache = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = CacheBenchmarkFixture.CreateObjectDefinitions(archiveCount: 1, includeArchives: true);
        _archiveReconstructionCache = _fixture.CreateCache(snapshotCapacityBytes: 0);
        _coldSnapshotCache = _fixture.CreateCache(snapshotCapacityBytes: 0);
        _warmSnapshotCache = _fixture.CreateCache(_fixture.SnapshotWeightPerArchive);

        _archiveReconstructionCache.ReadReferenceTable(CacheBenchmarkFixture.ObjectIndexId);
        _coldSnapshotCache.ReadReferenceTable(CacheBenchmarkFixture.ObjectIndexId);
        using var member = _warmSnapshotCache.Read(CacheBenchmarkFixture.ObjectIndexId, 0, 0);
    }

    [Benchmark(Baseline = true)]
    public long ArchiveReconstructionBaseline() => CacheBenchmarkReads.ReadArchiveMember(
        _archiveReconstructionCache,
        CacheBenchmarkFixture.ObjectIndexId,
        archiveId: 0,
        memberIndex: 0);

    [Benchmark]
    public long ColdSingleMemberRead() => CacheBenchmarkReads.ReadSnapshotMember(
        _coldSnapshotCache,
        CacheBenchmarkFixture.ObjectIndexId,
        archiveId: 0,
        memberIndex: 0);

    [Benchmark]
    public long WarmSingleMemberRead() => CacheBenchmarkReads.ReadSnapshotMember(
        _warmSnapshotCache,
        CacheBenchmarkFixture.ObjectIndexId,
        archiveId: 0,
        memberIndex: 0);

    [GlobalCleanup]
    public void Cleanup()
    {
        _archiveReconstructionCache.Dispose();
        _coldSnapshotCache.Dispose();
        _warmSnapshotCache.Dispose();
    }
}

[MemoryDiagnoser]
public class CacheSnapshotConcurrentReadBenchmarks
{
    private const int ReaderCount = 8;
    private CacheBenchmarkFixture _fixture = null!;
    private CacheApi _archiveReconstructionCache = null!;
    private CacheApi _coldSnapshotCache = null!;
    private CacheApi _warmSnapshotCache = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = CacheBenchmarkFixture.CreateRawArchives(
            indexId: 20,
            archiveCount: 1,
            memberCount: 1,
            memberSize: 256 * 1024);
        _archiveReconstructionCache = _fixture.CreateCache(snapshotCapacityBytes: 0);
        _coldSnapshotCache = _fixture.CreateCache(snapshotCapacityBytes: 0);
        _warmSnapshotCache = _fixture.CreateCache(_fixture.TotalSnapshotWeight);

        _archiveReconstructionCache.ReadReferenceTable(_fixture.IndexId);
        _coldSnapshotCache.ReadReferenceTable(_fixture.IndexId);
        using var member = _warmSnapshotCache.Read(_fixture.IndexId, 0, 0);
    }

    [Benchmark(Baseline = true)]
    public long ConcurrentArchiveReconstruction() => CacheBenchmarkReads.ReadConcurrently(
        _archiveReconstructionCache,
        _fixture.IndexId,
        archiveId: 0,
        memberIndex: 0,
        ReaderCount,
        useSnapshots: false);

    [Benchmark]
    public long ConcurrentColdSameArchiveRead() => CacheBenchmarkReads.ReadConcurrently(
        _coldSnapshotCache,
        _fixture.IndexId,
        archiveId: 0,
        memberIndex: 0,
        ReaderCount,
        useSnapshots: true);

    [Benchmark]
    public long ConcurrentWarmSameArchiveRead() => CacheBenchmarkReads.ReadConcurrently(
        _warmSnapshotCache,
        _fixture.IndexId,
        archiveId: 0,
        memberIndex: 0,
        ReaderCount,
        useSnapshots: true);

    [GlobalCleanup]
    public void Cleanup()
    {
        _archiveReconstructionCache.Dispose();
        _coldSnapshotCache.Dispose();
        _warmSnapshotCache.Dispose();
    }
}

[MemoryDiagnoser]
// This compares decoding every call with CacheApi's memoized read. The provider
// before #559 also memoized reference tables, so this is not a PR baseline.
public class CacheReferenceTableMemoizationBenchmarks
{
    private CacheBenchmarkFixture _fixture = null!;
    private CacheApi _memoizedCache = null!;
    private IReferenceTableProvider _directProvider = null!;
    private IFileStore _directStore = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = CacheBenchmarkFixture.CreateObjectDefinitions(archiveCount: 260, includeArchives: false);
        _memoizedCache = _fixture.CreateCache(snapshotCapacityBytes: 0);
        _ = _memoizedCache.ReadReferenceTable(CacheBenchmarkFixture.ObjectIndexId);

        _directStore = _fixture.CreateStore();
        _directProvider = new ReferenceTableProvider(
            _directStore,
            new ContainerDecoder(),
            new ReferenceTableCodec());
    }

    [Benchmark]
    public int DecodeReferenceTableWithoutMemoization() =>
        _directProvider.ReadReferenceTable(CacheBenchmarkFixture.ObjectIndexId).Count;

    [Benchmark]
    public int ReadMemoizedReferenceTable() =>
        _memoizedCache.ReadReferenceTable(CacheBenchmarkFixture.ObjectIndexId).Count;

    [GlobalCleanup]
    public void Cleanup()
    {
        _memoizedCache.Dispose();
        _directStore.Dispose();
    }
}

[MemoryDiagnoser]
public class CacheArchiveSnapshotLruBenchmarks
{
    private const int ArchiveCount = 3;
    private CacheBenchmarkFixture _fixture = null!;
    private CacheApi _archiveReconstructionCache = null!;
    private CacheApi _lruCache = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = CacheBenchmarkFixture.CreateObjectDefinitions(ArchiveCount, includeArchives: true);
        _archiveReconstructionCache = _fixture.CreateCache(snapshotCapacityBytes: 0);
        _lruCache = _fixture.CreateCache(2 * _fixture.SnapshotWeightPerArchive);
        _archiveReconstructionCache.ReadReferenceTable(CacheBenchmarkFixture.ObjectIndexId);
        _lruCache.ReadReferenceTable(CacheBenchmarkFixture.ObjectIndexId);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = ArchiveCount)]
    public long ReconstructThreeArchives() => ReadThreeArchives(
        _archiveReconstructionCache,
        useSnapshots: false);

    [Benchmark(OperationsPerInvoke = ArchiveCount)]
    public long EvictAndReloadThreeArchives() => ReadThreeArchives(
        _lruCache,
        useSnapshots: true);

    [GlobalCleanup]
    public void Cleanup()
    {
        _archiveReconstructionCache.Dispose();
        _lruCache.Dispose();
    }

    private static long ReadThreeArchives(CacheApi cache, bool useSnapshots)
    {
        long totalLength = 0;
        for (var archiveId = 0; archiveId < ArchiveCount; archiveId++)
        {
            totalLength += useSnapshots
                ? CacheBenchmarkReads.ReadSnapshotMember(cache, CacheBenchmarkFixture.ObjectIndexId, archiveId, memberIndex: 0)
                : CacheBenchmarkReads.ReadArchiveMember(cache, CacheBenchmarkFixture.ObjectIndexId, archiveId, memberIndex: 0);
        }

        return totalLength;
    }
}

[MemoryDiagnoser]
// The baseline mirrors the pre-snapshot ObjectTypeProvider.Get read/decode path.
// Its memoized reference-table reads match the provider behavior before #559.
public class CacheObjectDefinitionWorkloadBenchmarks
{
    private const int ObjectArchiveCount = 260;
    private const int LookupCount = CacheBenchmarkFixture.ObjectDefinitionLookupCount;
    private CacheBenchmarkFixture _fixture = null!;
    private CacheApi _legacyCache = null!;
    private ObjectTypeData _legacyTypeData = null!;
    private ObjectTypeCodec _legacyCodec = null!;
    private ObjectTypeFactory _legacyFactory = null!;
    private ObjectTypeProvider _zeroRetentionProvider = null!;
    private ObjectTypeProvider _snapshotProvider = null!;
    private CacheApi _zeroRetentionCache = null!;
    private CacheApi _snapshotCache = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = CacheBenchmarkFixture.CreateObjectDefinitions(ObjectArchiveCount, includeArchives: true);
        _legacyCache = _fixture.CreateCache(snapshotCapacityBytes: 0);
        _zeroRetentionCache = _fixture.CreateCache(snapshotCapacityBytes: 0);
        _snapshotCache = _fixture.CreateCache(_fixture.TotalSnapshotWeight);
        _legacyTypeData = new ObjectTypeData();
        _legacyCodec = new ObjectTypeCodec();
        _legacyFactory = new ObjectTypeFactory();
        _zeroRetentionProvider = CreateProvider(_zeroRetentionCache);
        _snapshotProvider = CreateProvider(_snapshotCache);

        _legacyCache.ReadReferenceTable(CacheBenchmarkFixture.ObjectIndexId);
        _zeroRetentionCache.ReadReferenceTable(CacheBenchmarkFixture.ObjectIndexId);
        for (var archiveId = 0; archiveId < ObjectArchiveCount; archiveId++)
        {
            using var member = _snapshotCache.Read(CacheBenchmarkFixture.ObjectIndexId, archiveId, 0);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = LookupCount)]
    public long LegacyObjectDefinitionWorkload() => RunLegacyLookupWorkload(_fixture.ObjectTypeIds);

    [Benchmark(OperationsPerInvoke = LookupCount)]
    public long ZeroRetentionObjectDefinitionWorkload() => RunLookupWorkload(_zeroRetentionProvider, _fixture.ObjectTypeIds);

    [Benchmark(OperationsPerInvoke = LookupCount)]
    public long WarmSnapshotObjectDefinitionWorkload() => RunLookupWorkload(_snapshotProvider, _fixture.ObjectTypeIds);

    [GlobalCleanup]
    public void Cleanup()
    {
        _legacyCache.Dispose();
        _zeroRetentionCache.Dispose();
        _snapshotCache.Dispose();
    }

    private static ObjectTypeProvider CreateProvider(CacheApi cache) =>
        new(cache, new ObjectTypeCodec(), new ObjectTypeFactory());

    private static long RunLookupWorkload(ObjectTypeProvider provider, int[] typeIds)
    {
        long checksum = 0;
        for (var index = 0; index < typeIds.Length; index++)
        {
            checksum += provider.Get(typeIds[index]).Id;
        }

        return checksum;
    }

    private long RunLegacyLookupWorkload(int[] typeIds)
    {
        long checksum = 0;
        for (var index = 0; index < typeIds.Length; index++)
        {
            var typeId = typeIds[index];
            var table = _legacyCache.ReadReferenceTable(_legacyTypeData.IndexId);
            var archiveId = _legacyTypeData.GetArchiveId(typeId);
            var entry = table.GetEntry(archiveId, _legacyTypeData.GetArchiveEntryId(typeId));
            if (entry is null)
            {
                checksum += _legacyFactory.CreateType(typeId).Id;
                continue;
            }

            var archive = _legacyCache.ReadArchive(_legacyTypeData.IndexId, archiveId);
            checksum += _legacyCodec.Decode(typeId, archive.GetEntry(entry.Index)).Id;
        }

        return checksum;
    }
}

internal static class CacheBenchmarkReads
{
    public static long ReadArchiveMember(CacheApi cache, int indexId, int archiveId, int memberIndex)
    {
        using var archive = cache.ReadArchive(indexId, archiveId);
        return archive.GetEntry(memberIndex).Length;
    }

    public static long ReadSnapshotMember(CacheApi cache, int indexId, int archiveId, int memberIndex)
    {
        using var stream = cache.Read(indexId, archiveId, memberIndex);
        return stream.Length;
    }

    public static long ReadConcurrently(
        CacheApi cache,
        int indexId,
        int archiveId,
        int memberIndex,
        int readerCount,
        bool useSnapshots)
    {
        long totalLength = 0;
        Parallel.For(0, readerCount, readerIndex =>
        {
            long length;
            if (useSnapshots)
            {
                using var member = cache.Read(indexId, archiveId, memberIndex);
                length = member.Length;
            }
            else
            {
                using var archive = cache.ReadArchive(indexId, archiveId);
                length = archive.GetEntry(memberIndex).Length;
            }

            Interlocked.Add(ref totalLength, length);
        });

        return totalLength;
    }
}

internal sealed class CacheBenchmarkFixture
{
    public const int ObjectIndexId = 16;
    public const int ObjectMembersPerArchive = 256;
    public const int ObjectDefinitionLookupCount = 19_967;

    private readonly Dictionary<(int IndexId, int FileId), byte[]> _files;

    private CacheBenchmarkFixture(
        int indexId,
        int archiveCount,
        long snapshotWeightPerArchive,
        Dictionary<(int IndexId, int FileId), byte[]> files,
        int[] objectTypeIds)
    {
        IndexId = indexId;
        ArchiveCount = archiveCount;
        SnapshotWeightPerArchive = snapshotWeightPerArchive;
        _files = files;
        ObjectTypeIds = objectTypeIds;
    }

    public int IndexId { get; }
    public int ArchiveCount { get; }
    public long SnapshotWeightPerArchive { get; }
    public long TotalSnapshotWeight => SnapshotWeightPerArchive * ArchiveCount;
    public int[] ObjectTypeIds { get; }

    public static CacheBenchmarkFixture CreateObjectDefinitions(int archiveCount, bool includeArchives)
    {
        var objectCodec = new ObjectTypeCodec();
        var members = new byte[ObjectMembersPerArchive][];
        for (var memberId = 0; memberId < ObjectMembersPerArchive; memberId++)
        {
            var definition = new ObjectType(memberId) { Name = $"Object {memberId:D3}" };
            definition.Actions[0] = "Open";
            using var encoded = objectCodec.Encode(definition);
            members[memberId] = encoded.ToArray();
        }

        return CreateFixture(
            ObjectIndexId,
            archiveCount,
            members,
            includeArchives,
            objectTypeIds: CreateObjectTypeWorkload(archiveCount));
    }

    public static CacheBenchmarkFixture CreateRawArchives(int indexId, int archiveCount, int memberCount, int memberSize)
    {
        var members = new byte[memberCount][];
        for (var memberId = 0; memberId < memberCount; memberId++)
        {
            members[memberId] = new byte[memberSize];
            new Random(506 + memberId).NextBytes(members[memberId]);
        }

        return CreateFixture(indexId, archiveCount, members, includeArchives: true, objectTypeIds: []);
    }

    public CacheApi CreateCache(long snapshotCapacityBytes)
    {
        var store = CreateStore();
        var containerDecoder = new ContainerDecoder();
        var referenceTableCodec = new ReferenceTableCodec();
        var referenceTableProvider = new ReferenceTableProvider(store, containerDecoder, referenceTableCodec);
        var cacheWriter = new CacheWriter(store, referenceTableProvider, containerDecoder, referenceTableCodec);

        return new CacheApi(
            store,
            referenceTableProvider,
            cacheWriter,
            containerDecoder,
            referenceTableCodec,
            new ArchiveDecoder(),
            Options.Create(new CacheOptions { ArchiveSnapshotCacheSizeBytes = snapshotCapacityBytes }));
    }

    public IFileStore CreateStore() => new BenchmarkFileStore(_files);

    private static CacheBenchmarkFixture CreateFixture(
        int indexId,
        int archiveCount,
        byte[][] members,
        bool includeArchives,
        int[] objectTypeIds)
    {
        var files = new Dictionary<(int IndexId, int FileId), byte[]>();
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, archiveCount);
        var archiveData = includeArchives ? EncodeArchive(members) : null;
        var encodedArchive = archiveData is null ? null : EncodeContainer(CompressionType.Gzip, archiveData);
        for (var archiveId = 0; archiveId < archiveCount; archiveId++)
        {
            var archiveEntry = new ReferenceTableEntry(archiveId);
            archiveEntry.InitializeEntries(members.Length);
            for (var memberId = 0; memberId < members.Length; memberId++)
            {
                archiveEntry.AddEntry(memberId, new ReferenceTableChildEntry(memberId));
            }

            table.AddEntry(archiveId, archiveEntry);
            if (includeArchives)
            {
                files[(indexId, archiveId)] = encodedArchive!;
            }
        }

        var referenceTableCodec = new ReferenceTableCodec();
        using var referenceTableData = referenceTableCodec.Encode(table);
        files[(255, indexId)] = EncodeContainer(CompressionType.Gzip, referenceTableData.ToArray());

        var snapshotWeight = checked(members.Sum(member => (long)member.Length) + (members.Length * sizeof(int) * 2L) + 128);
        return new CacheBenchmarkFixture(indexId, archiveCount, snapshotWeight, files, objectTypeIds);
    }

    private static byte[] EncodeArchive(byte[][] members)
    {
        if (members.Length == 1)
        {
            return members[0];
        }

        using var stream = new MemoryStream();
        foreach (var member in members)
        {
            stream.Write(member);
        }

        var previousMemberLength = 0;
        Span<byte> chunkLength = stackalloc byte[sizeof(int)];
        foreach (var member in members)
        {
            BinaryPrimitives.WriteInt32BigEndian(chunkLength, member.Length - previousMemberLength);
            stream.Write(chunkLength);
            previousMemberLength = member.Length;
        }

        stream.WriteByte(1);
        return stream.ToArray();
    }

    private static byte[] EncodeContainer(CompressionType compressionType, byte[] data)
    {
        using var dataStream = new MemoryStream(data, writable: false);
        using var container = new Container(compressionType, dataStream);
        return container.Encode();
    }

    private static int[] CreateObjectTypeWorkload(int archiveCount)
    {
        if (archiveCount <= 16)
        {
            return [];
        }

        var typeIds = new int[ObjectDefinitionLookupCount];
        var random = new Random(506);
        for (var index = 0; index < typeIds.Length; index++)
        {
            var archiveId = index % 4 == 0
                ? 16 + random.Next(archiveCount - 16)
                : random.Next(16);
            var memberId = random.Next(ObjectMembersPerArchive);
            typeIds[index] = archiveId * ObjectMembersPerArchive + memberId;
        }

        return typeIds;
    }
}

internal sealed class BenchmarkFileStore : IFileStore
{
    private readonly Dictionary<(int IndexId, int FileId), byte[]> _files;

    public BenchmarkFileStore(Dictionary<(int IndexId, int FileId), byte[]> files) => _files = new(files);

    public int IndexFileCount => 255;

    public int GetFileCount(int indexId) => _files.Keys.Count(key => key.IndexId == indexId);

    public MemoryStream Read(int indexId, int fileId)
    {
        if (!_files.TryGetValue((indexId, fileId), out var data))
        {
            throw new FileNotFoundException($"Benchmark cache file {indexId}:{fileId} was not found.");
        }

        return new MemoryStream(data, writable: false);
    }

    public void Write(int indexId, int fileId, MemoryStream data) => _files[(indexId, fileId)] = data.ToArray();

    public void Dispose()
    {
    }
}
