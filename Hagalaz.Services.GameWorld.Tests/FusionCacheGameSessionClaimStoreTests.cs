using Microsoft.Extensions.Logging;
using NSubstitute;
using Hagalaz.Services.GameWorld.Services;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Locking.Distributed;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class FusionCacheGameSessionClaimStoreTests
{
    [TestMethod]
    public async Task TryClaimAsync_MissingClaimStoresValueWithLeaseAndSkipsMemoryCache()
    {
        var (store, cache, locker) = CreateStore();
        FusionCacheEntryOptions? options = null;
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(MaybeValue<string>.None));
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<FusionCacheEntryOptions>(value => options = value),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        Assert.IsTrue(await store.TryClaimAsync(42, "claim"));
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "claim",
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        await locker.Received(1).AcquireLockAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            "hagalaz:game-session:42",
            Arg.Any<string>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<ILogger>(),
            Arg.Any<CancellationToken>());
        Assert.IsNotNull(options);
        Assert.IsTrue(options!.SkipMemoryCacheRead);
        Assert.IsTrue(options.SkipMemoryCacheWrite);
        Assert.AreEqual(GameSessionClaimOptions.LeaseDuration, options.Duration);
        Assert.AreEqual(GameSessionClaimOptions.LeaseDuration, options.DistributedCacheDuration);
    }

    [TestMethod]
    public async Task TryClaimAsync_ExistingClaimDoesNotOverwriteAnotherOwner()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(MaybeValue<string>.FromValue("existing")));

        Assert.IsFalse(await store.TryClaimAsync(42, "replacement"));
        await cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ReleaseAsync_RemovesOnlyExactOwner()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(MaybeValue<string>.FromValue("owner")));

        Assert.IsFalse(await store.ReleaseAsync(42, "other"));
        await cache.DidNotReceive().RemoveAsync(
            Arg.Any<string>(),
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());

        Assert.IsTrue(await store.ReleaseAsync(42, "owner"));
        await cache.Received(1).RemoveAsync(
            "hagalaz:game-session:42",
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task RenewAsync_RefreshesOnlyExactOwner()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(MaybeValue<string>.FromValue("owner")));

        Assert.IsTrue(await store.RenewAsync(42, "owner"));
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "owner",
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExecuteIfOwnerAsync_RenewsExactOwnerAndPreservesSuccessfulCallbackWhenLockReleaseFails()
    {
        var (store, cache, locker) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(MaybeValue<string>.FromValue("owner")));
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<FusionCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        locker.ReleaseLockAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<ILogger>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromException(new InvalidOperationException("Lock release failed.")));

        var callbackCalled = false;
        var result = await store.ExecuteIfOwnerAsync(42, "owner", _ =>
        {
            callbackCalled = true;
            return Task.FromResult(true);
        });

        Assert.IsTrue(result);
        Assert.IsTrue(callbackCalled);
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "owner",
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        await locker.Received(1).ReleaseLockAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            "hagalaz:game-session:42",
            Arg.Any<string>(),
            Arg.Any<object>(),
            Arg.Any<ILogger>(),
            CancellationToken.None);
    }

    [TestMethod]
    public async Task TryClaimAsync_PropagatesCancellationTokenToLockAndCacheOperations()
    {
        var (store, cache, locker) = CreateStore();
        var cancellationToken = new CancellationTokenSource().Token;
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), cancellationToken)
            .Returns(ValueTask.FromResult(MaybeValue<string>.None));
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<FusionCacheEntryOptions>(),
                cancellationToken)
            .Returns(ValueTask.CompletedTask);

        Assert.IsTrue(await store.TryClaimAsync(42, "claim", cancellationToken));

        await locker.Received(1).AcquireLockAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            "hagalaz:game-session:42",
            Arg.Any<string>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<ILogger>(),
            cancellationToken);
        await cache.Received(1).TryGetAsync<string>(
            "hagalaz:game-session:42",
            Arg.Any<FusionCacheEntryOptions>(),
            cancellationToken);
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "claim",
            Arg.Any<FusionCacheEntryOptions>(),
            cancellationToken);
        await locker.Received(1).ReleaseLockAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            "hagalaz:game-session:42",
            Arg.Any<string>(),
            Arg.Any<object>(),
            Arg.Any<ILogger>(),
            CancellationToken.None);
    }

    private static (FusionCacheGameSessionClaimStore Store, IFusionCache Cache, IFusionCacheDistributedLocker Locker) CreateStore()
    {
        var cache = Substitute.For<IFusionCache>();
        var locker = Substitute.For<IFusionCacheDistributedLocker>();
        locker.AcquireLockAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<ILogger>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object>(new object()));
        locker.ReleaseLockAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<ILogger>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        return (
            new FusionCacheGameSessionClaimStore(
                cache,
                locker,
                Substitute.For<ILogger<FusionCacheGameSessionClaimStore>>()),
            cache,
            locker);
    }
}
