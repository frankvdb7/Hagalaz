using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Cache;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CachedGameObjectServiceTests
{
    [TestMethod]
    public async Task FindDefinitionsByIds_DeduplicatesRequestedIdsAndCachesTheBatch()
    {
        var overrides = new Dictionary<uint, GameObjectDefinitionOverride>
        {
            [100] = new(100, "override-100", 11),
            [200] = new(200, "override-200", null),
            [999] = new(999, "unrequested", 99)
        };
        var (service, repository, _) = CreateService(overrides);

        var result = await service.FindGameObjectDefinitionsByIdsAsync([100, 100, 200, 300, 400]);

        Assert.AreEqual(4, result.Count);
        Assert.IsTrue(result.ContainsKey(100));
        Assert.IsTrue(result.ContainsKey(200));
        Assert.IsTrue(result.ContainsKey(300));
        Assert.IsTrue(result.ContainsKey(400));
        Assert.IsFalse(result.ContainsKey(999));
        Assert.AreEqual("override-100", result[100].Examine);
        Assert.AreEqual(11, result[100].LootTableId);
        Assert.AreEqual("override-200", result[200].Examine);
        Assert.AreEqual(0, result[200].LootTableId);
        Assert.AreEqual("archive-300", result[300].Examine);
        Assert.AreEqual("archive-400", result[400].Examine);

        await repository.Received(1).FindOverridesByIdsAsync(
            Arg.Is<IReadOnlyCollection<uint>>(ids => ids != null && ids.OrderBy(id => id).SequenceEqual(new uint[] { 100, 200, 300, 400 })),
            Arg.Any<CancellationToken>());

        var repeated = await service.FindGameObjectDefinitionsByIdsAsync([400, 300, 200, 100]);
        Assert.AreEqual(4, repeated.Count);
        await repository.Received(1).FindOverridesByIdsAsync(
            Arg.Any<IReadOnlyCollection<uint>>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task FindDefinitionById_RetainsTheSingleIdCachedPath()
    {
        var (service, repository, _) = CreateService(new Dictionary<uint, GameObjectDefinitionOverride>
        {
            [100] = new(100, "override-100", 11)
        });

        var first = await service.FindGameObjectDefinitionById(100);
        var second = await service.FindGameObjectDefinitionById(100);

        Assert.AreSame(first, second);
        Assert.AreEqual("override-100", first.Examine);
        await repository.Received(1).FindOverridesByIdsAsync(
            Arg.Is<IReadOnlyCollection<uint>>(ids => ids != null && ids.SequenceEqual(new uint[] { 100 })),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task FindDefinitionsByIds_WithEmptyInputReturnsEmptyWithoutDatabaseQuery()
    {
        var (service, repository, _) = CreateService([]);

        var result = await service.FindGameObjectDefinitionsByIdsAsync([]);

        Assert.AreEqual(0, result.Count);
        await repository.DidNotReceive().FindOverridesByIdsAsync(
            Arg.Any<IReadOnlyCollection<uint>>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task FindDefinitionsByIds_PropagatesCancellationToBulkRead()
    {
        using var source = new CancellationTokenSource();
        var repository = Substitute.For<IGameObjectDefinitionRepository>();
        repository.FindOverridesByIdsAsync(Arg.Any<IReadOnlyCollection<uint>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                source.Cancel();
                return Task.FromCanceled<Dictionary<uint, GameObjectDefinitionOverride>>(token);
            });
        var inner = new GameObjectService(
            Substitute.For<IMapRegionService>(),
            repository,
            CreateDefinitionProvider());
        var service = new CachedGameObjectService(inner, new SharedHybridCache());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.FindGameObjectDefinitionsByIdsAsync([100], source.Token));

        await repository.Received(1).FindOverridesByIdsAsync(
            Arg.Is<IReadOnlyCollection<uint>>(ids => ids != null && ids.SequenceEqual(new uint[] { 100 })),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void Build_UsesProvidedDefinitionAndPreservesSingleIdFallback()
    {
        var gameObjectService = Substitute.For<IGameObjectService>();
        var scriptProvider = Substitute.For<IGameObjectScriptProvider>();
        scriptProvider.GetGameObjectScriptTypeById(100).Returns(typeof(IGameObjectScript));
        var script = Substitute.For<IGameObjectScript>();
        using var services = new ServiceCollection()
            .AddSingleton<IGameObjectService>(gameObjectService)
            .AddScoped<IGameObjectScript>(_ => script)
            .BuildServiceProvider();
        var definition = Substitute.For<IGameObjectDefinition>();
        var builder = new GameObjectBuilder(services, scriptProvider);

        var gameObject = builder.Create()
            .WithId(100)
            .WithLocation(Location.Create(3200, 3200, 0, 0))
            .WithDefinition(definition)
            .Build();

        Assert.AreSame(definition, gameObject.Definition);
        gameObjectService.DidNotReceive().FindGameObjectDefinitionById(Arg.Any<int>(), Arg.Any<CancellationToken>());

        var fallbackDefinition = Substitute.For<IGameObjectDefinition>();
        gameObjectService.FindGameObjectDefinitionById(100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(fallbackDefinition));
        var fallbackObject = builder.Create()
            .WithId(100)
            .WithLocation(Location.Create(3200, 3200, 0, 0))
            .Build();

        Assert.AreSame(fallbackDefinition, fallbackObject.Definition);
        gameObjectService.Received(1).FindGameObjectDefinitionById(100, Arg.Any<CancellationToken>());
    }

    private static (CachedGameObjectService Service, IGameObjectDefinitionRepository Repository, Dictionary<int, GameObjectDefinition> Definitions)
        CreateService(Dictionary<uint, GameObjectDefinitionOverride> overrides)
    {
        var repository = Substitute.For<IGameObjectDefinitionRepository>();
        repository.FindOverridesByIdsAsync(Arg.Any<IReadOnlyCollection<uint>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var ids = callInfo.Arg<IReadOnlyCollection<uint>>().ToHashSet();
                var matchingOverrides = overrides.Where(pair => ids.Contains(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                return Task.FromResult(matchingOverrides);
            });
        var definitions = new[] { 100, 200, 300, 400 }.ToDictionary(
            id => id,
            id => new GameObjectDefinition(id) { Examine = $"archive-{id}" });
        var definitionProvider = Substitute.For<ITypeProvider<GameObjectDefinition>>();
        definitionProvider.Get(Arg.Any<int>()).Returns(call => definitions[call.Arg<int>()]);
        var inner = new GameObjectService(Substitute.For<IMapRegionService>(), repository, definitionProvider);
        return (new CachedGameObjectService(inner, new SharedHybridCache()), repository, definitions);
    }

    private static ITypeProvider<GameObjectDefinition> CreateDefinitionProvider()
    {
        var definitionProvider = Substitute.For<ITypeProvider<GameObjectDefinition>>();
        definitionProvider.Get(Arg.Any<int>()).Returns(call => new GameObjectDefinition(call.Arg<int>()));
        return definitionProvider;
    }

    private sealed class SharedHybridCache : HybridCache
    {
        private readonly Dictionary<string, object> _entries = new();

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options,
            IEnumerable<string>? tags,
            CancellationToken cancellationToken)
        {
            if (_entries.TryGetValue(key, out var cached))
            {
                return ValueTask.FromResult((T)cached);
            }

            return CreateAsync();

            async ValueTask<T> CreateAsync()
            {
                var value = await factory(state, cancellationToken);
                _entries[key] = value!;
                return value;
            }
        }

        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options, IEnumerable<string>? tags, CancellationToken cancellationToken)
        {
            _entries[key] = value!;
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken)
        {
            _entries.Remove(key);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
