using Hagalaz.Services.GameWorld.Store;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Locking.Distributed;

namespace Hagalaz.Services.GameWorld.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class FusionCacheGameSessionClaimIntegrationTests
{
    private static RedisContainer? _redis;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _redis = new RedisBuilder("redis:7.4").Build();
        await _redis.StartAsync();
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_redis != null)
        {
            await _redis.DisposeAsync();
        }
    }

    [TestMethod]
    [Timeout(120000)]
    public void Startup_ResolvesInterfaceAndConcreteStoreAsTheSameSingleton()
    {
        using var provider = CreateProvider();

        var concreteStore = provider.GetRequiredService<GameSessionStore>();
        var interfaceStore = provider.GetRequiredService<IGameSessionStore>();

        Assert.AreSame(concreteStore, interfaceStore);
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task TryClaimAsync_IsAtomicAcrossConcurrentWorldProviders()
    {
        await using var firstProvider = CreateProvider();
        await using var secondProvider = CreateProvider();
        var firstStore = firstProvider.GetRequiredService<IGameSessionClaimStore>();
        var secondStore = secondProvider.GetRequiredService<IGameSessionClaimStore>();
        await ClearClaimAsync(firstProvider, 42);

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Task.Run(async () =>
        {
            await start.Task;
            return await firstStore.TryClaimAsync(42, "world-1");
        });
        var second = Task.Run(async () =>
        {
            await start.Task;
            return await secondStore.TryClaimAsync(42, "world-2");
        });
        start.SetResult();

        var results = await Task.WhenAll(first, second);

        Assert.AreEqual(1, results.Count(result => result));
        var cache = firstProvider.GetRequiredService<IFusionCache>();
        var current = await cache.TryGetAsync<string>(Key(42), EntryOptions());
        Assert.IsTrue(current.HasValue);
        Assert.IsTrue(current.Value is "world-1" or "world-2");
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task ExecuteIfOwnerAsync_HoldsClaimLockDuringPromotionCallback()
    {
        await using var firstProvider = CreateProvider();
        await using var secondProvider = CreateProvider();
        var firstStore = firstProvider.GetRequiredService<IGameSessionClaimStore>();
        var secondStore = secondProvider.GetRequiredService<IGameSessionClaimStore>();
        const uint masterId = 46;
        await ClearClaimAsync(firstProvider, masterId);

        Assert.IsTrue(await firstStore.TryClaimAsync(masterId, "world-1"));
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ownerOperation = firstStore.ExecuteIfOwnerAsync(masterId, "world-1", async _ =>
        {
            callbackStarted.SetResult();
            await releaseCallback.Task;
            return true;
        });
        await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var replacementOperation = secondStore.TryClaimAsync(masterId, "world-2");
        var completed = await Task.WhenAny(replacementOperation, Task.Delay(500));
        Assert.AreNotSame(replacementOperation, completed);

        releaseCallback.SetResult();
        Assert.IsTrue(await ownerOperation);
        Assert.IsFalse(await replacementOperation);
        Assert.IsTrue(await firstStore.ReleaseAsync(masterId, "world-1"));
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task ReleaseAndRenewAsync_RequireExactClaimOwner()
    {
        await using var firstProvider = CreateProvider();
        await using var secondProvider = CreateProvider();
        var firstStore = firstProvider.GetRequiredService<IGameSessionClaimStore>();
        var secondStore = secondProvider.GetRequiredService<IGameSessionClaimStore>();
        await ClearClaimAsync(firstProvider, 43);

        Assert.IsTrue(await firstStore.TryClaimAsync(43, "owner"));
        Assert.IsFalse(await secondStore.RenewAsync(43, "other-owner"));
        Assert.IsFalse(await secondStore.ReleaseAsync(43, "other-owner"));
        Assert.IsTrue((await secondProvider.GetRequiredService<IFusionCache>().TryGetAsync<string>(Key(43), EntryOptions())).HasValue);
        Assert.IsTrue(await secondStore.RenewAsync(43, "owner"));
        Assert.IsTrue(await secondStore.ReleaseAsync(43, "owner"));
        Assert.IsFalse((await firstProvider.GetRequiredService<IFusionCache>().TryGetAsync<string>(Key(43), EntryOptions())).HasValue);
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task ClaimExpiresAndCanBeReclaimedAfterWorldCrash()
    {
        await using var provider = CreateProvider();
        var store = provider.GetRequiredService<IGameSessionClaimStore>();
        var cache = provider.GetRequiredService<IFusionCache>();
        const uint masterId = 44;
        await ClearClaimAsync(provider, masterId);

        Assert.IsTrue(await store.TryClaimAsync(masterId, "crashed-world"));
        await cache.SetAsync(Key(masterId), "crashed-world", new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMilliseconds(100),
            DistributedCacheDuration = TimeSpan.FromMilliseconds(100),
            IsFailSafeEnabled = false,
            SkipMemoryCacheRead = true,
            SkipMemoryCacheWrite = true
        });

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while ((await cache.TryGetAsync<string>(Key(masterId), EntryOptions(), timeout.Token)).HasValue)
        {
            await Task.Delay(25, timeout.Token);
        }

        Assert.IsTrue(await store.TryClaimAsync(masterId, "replacement-world"));
    }

    [TestMethod]
    [Timeout(120000)]
    public async Task StartupResolvesFusionCacheDistributedClaimStoreFromConfiguredCacheConnection()
    {
        await using var provider = CreateProvider();

        Assert.IsNotNull(provider.GetRequiredService<IDistributedCache>());
        Assert.IsNotNull(provider.GetRequiredService<IFusionCache>());
        Assert.IsNotNull(provider.GetRequiredService<IFusionCacheDistributedLocker>());
        var claimStore = provider.GetRequiredService<IGameSessionClaimStore>();
        Assert.IsInstanceOfType<FusionCacheGameSessionClaimStore>(claimStore);
        Assert.IsTrue(await claimStore.TryClaimAsync(45, "startup-world"));
        Assert.IsTrue(await claimStore.ReleaseAsync(45, "startup-world"));
    }

    private static ServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:cache"] = _redis!.GetConnectionString()
            })
            .Build();
        var services = new ServiceCollection();
        new Hagalaz.Services.GameWorld.Startup(configuration).ConfigureServices(services);
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static async Task ClearClaimAsync(ServiceProvider provider, uint masterId) =>
        await provider.GetRequiredService<IFusionCache>().RemoveAsync(Key(masterId), EntryOptions());

    private static FusionCacheEntryOptions EntryOptions() => new()
    {
        IsFailSafeEnabled = false,
        SkipMemoryCacheRead = true,
        SkipMemoryCacheWrite = true
    };

    private static string Key(uint masterId) => $"hagalaz:game-session:{masterId}";
}
