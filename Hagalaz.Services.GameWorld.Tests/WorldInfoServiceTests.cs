using System.Collections.Generic;
using AutoMapper;
using Hagalaz.Services.GameWorld.Profiles;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldInfoServiceTests
{
    [TestMethod]
    public async Task WorldInfoService_PreservesConfiguredEndpointAndAvailability()
    {
        var service = new WorldInfoService(
            new WorldInfoStore(),
            new MapperConfiguration(config => config.AddProfile<WorldProfile>(), LoggerFactory.Create(_ => { })).CreateMapper(),
            Substitute.For<HybridCache>());

        await service.AddOrUpdateWorldInfoAsync(CreateWorldInfo(1, 443));
        var initial = await service.FindAllWorldInfoAsync();
        Assert.AreEqual(443, initial.Single().Port);

        await service.UpdateWorldCharacterInfoAsync(new WorldCharacterInfo(1, 10, true));
        var availabilityChanged = await service.FindAllWorldCharacterInfoAsync();
        Assert.IsTrue(availabilityChanged.Single().Online);

        await service.AddOrUpdateWorldInfoAsync(CreateWorldInfo(1, 444));
        var endpointChanged = await service.FindAllWorldInfoAsync();
        Assert.AreEqual(444, endpointChanged.Single().Port);
    }

    [TestMethod]
    public async Task WorldInfoCache_UsesStableFingerprintAcrossServiceRestarts()
    {
        var sharedCache = new SharedHybridCache();
        var firstService = CreateService(sharedCache);
        await firstService.AddOrUpdateWorldInfoAsync(CreateWorldInfo(1, 443));

        var first = await firstService.GetCacheAsync();

        var restartedService = CreateService(sharedCache);
        await restartedService.AddOrUpdateWorldInfoAsync(CreateWorldInfo(1, 443));

        var afterRestart = await restartedService.GetCacheAsync();

        Assert.AreEqual(first.Checksum, afterRestart.Checksum);
        Assert.AreEqual(1, sharedCache.FactoryCalls);

        var changedService = CreateService(sharedCache);
        await changedService.AddOrUpdateWorldInfoAsync(CreateWorldInfo(1, 444));

        var changed = await changedService.GetCacheAsync();

        Assert.AreNotEqual(first.Checksum, changed.Checksum);
        Assert.AreEqual(2, sharedCache.FactoryCalls);
        Assert.AreEqual(444, changed.WorldInfos.Single().Port);
    }

    private static WorldInfoService CreateService(HybridCache cache) => new(
        new WorldInfoStore(),
        new MapperConfiguration(config => config.AddProfile<WorldProfile>(), LoggerFactory.Create(_ => { })).CreateMapper(),
        cache);

    private static WorldInfo CreateWorldInfo(int id, int port) => new()
    {
        Id = id,
        Name = $"World {id}",
        IpAddress = "world.example.test",
        Port = port,
        Location = new WorldLocationInfo("Local", 0),
        Settings = new WorldSettingsInfo
        {
            IsMembersOnly = false,
            IsQuickChatEnabled = true,
            IsPvP = false,
            IsLootShareEnabled = false,
            IsHighLighted = false
        }
    };

    private sealed class SharedHybridCache : HybridCache
    {
        private readonly Dictionary<string, object> _entries = new();

        public int FactoryCalls { get; private set; }

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options,
            IEnumerable<string>? tags,
            CancellationToken cancellationToken)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                return ValueTask.FromResult((T)entry);
            }

            return CreateAsync();

            async ValueTask<T> CreateAsync()
            {
                var value = await factory(state, cancellationToken);
                _entries[key] = value!;
                FactoryCalls++;
                return value;
            }
        }

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options,
            IEnumerable<string>? tags,
            CancellationToken cancellationToken)
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
