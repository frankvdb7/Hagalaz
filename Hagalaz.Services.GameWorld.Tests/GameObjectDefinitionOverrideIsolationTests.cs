using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Hooks;
using Hagalaz.Cache.Types.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Cache;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class GameObjectDefinitionOverrideIsolationTests
{
    [TestMethod]
    public async Task FindDefinitionsByIds_DecodesFromCacheSnapshotAndAppliesDatabaseOverridesToFreshDefinitions()
    {
        const int objectId = 259;
        const int indexId = 16;
        const int archiveId = 1;
        var store = new MemoryFileStore();
        var referenceTableCodec = new ReferenceTableCodec();
        var definitionCodec = new GameObjectDefinitionCodec(new ObjectTypeCodec());

        using var encodedDefinition = definitionCodec.Encode(new GameObjectDefinition(objectId) { Name = "archive" });
        store.Set(indexId, archiveId, EncodeContainer(encodedDefinition.ToArray()));
        store.Set(255, indexId, EncodeReferenceTable(referenceTableCodec, archiveId, objectId & 0xff));

        var services = new ServiceCollection();
        services.AddSingleton<IFileStore>(store);
        services.AddGameCache();
        services.AddTransient<ITypeFactory<GameObjectDefinition>, GameObjectDefinitionFactory>();
        services.AddTransient<ITypeCodec<GameObjectDefinition>, GameObjectDefinitionCodec>();
        services.AddTransient<ITypeEventHook<GameObjectDefinition>, ObjectTypeEventHook>();
        services.AddTransient<ITypeProvider<GameObjectDefinition>>(provider =>
            new TypeProvider<GameObjectDefinition, ObjectTypeData>(
                provider.GetRequiredService<ICacheAPI>(),
                provider.GetRequiredService<ITypeFactory<GameObjectDefinition>>(),
                provider.GetRequiredService<ITypeCodec<GameObjectDefinition>>(),
                provider.GetRequiredService<ITypeEventHook<GameObjectDefinition>>()));

        using var serviceProvider = services.BuildServiceProvider();
        var repository = Substitute.For<IGameObjectDefinitionRepository>();
        repository.FindOverridesByIdsAsync(Arg.Any<IReadOnlyCollection<uint>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new Dictionary<uint, GameObjectDefinitionOverride>
            {
                [objectId] = new GameObjectDefinitionOverride(objectId, "database", 42)
            }));
        var service = new GameObjectService(
            Substitute.For<IMapRegionService>(),
            repository,
            serviceProvider.GetRequiredService<ITypeProvider<GameObjectDefinition>>());

        var firstResult = await service.FindGameObjectDefinitionsByIdsAsync([objectId]);
        var first = firstResult[objectId];
        Assert.AreEqual("archive", first.Name);
        Assert.AreEqual("database", first.Examine);
        Assert.AreEqual(42, first.LootTableId);

        first.Examine = "caller mutation";
        var secondResult = await service.FindGameObjectDefinitionsByIdsAsync([objectId]);
        var second = secondResult[objectId];

        Assert.AreNotSame(first, second);
        Assert.AreEqual("archive", second.Name);
        Assert.AreEqual("database", second.Examine);
        Assert.AreEqual(42, second.LootTableId);
        Assert.AreEqual(1, store.GetReadCount(indexId, archiveId), "The archive should be loaded once and served from the CacheApi snapshot on the second lookup.");
    }

    private static byte[] EncodeReferenceTable(ReferenceTableCodec codec, int archiveId, int childId)
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, archiveId + 1);
        var archive = new ReferenceTableEntry(archiveId);
        archive.InitializeEntries(1);
        archive.AddEntry(childId, new ReferenceTableChildEntry(0));
        table.AddEntry(archiveId, archive);

        using var encoded = codec.Encode(table);
        return EncodeContainer(encoded.ToArray());
    }

    private static byte[] EncodeContainer(byte[] data)
    {
        using var container = new Container(new MemoryStream(data));
        return container.Encode();
    }

    private sealed class MemoryFileStore : IFileStore
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
}
