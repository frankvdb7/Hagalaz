#pragma warning disable CA2012 // NSubstitute configures ValueTask-returning members through the call specification.

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
    public async Task AllocateSessionGenerationAsync_UsesStableDistributedCounter()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<long>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(
                new ValueTask<MaybeValue<long>>(MaybeValue<long>.None),
                new ValueTask<MaybeValue<long>>(MaybeValue<long>.FromValue(7L)));
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<long>(),
                Arg.Any<FusionCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask());

        Assert.AreEqual(1L, await store.AllocateSessionGenerationAsync(42));
        Assert.AreEqual(8L, await store.AllocateSessionGenerationAsync(42));
        await cache.Received(1).SetAsync(
            "hagalaz:game-session-generation:42",
            1L,
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        await cache.Received(1).SetAsync(
            "hagalaz:game-session-generation:42",
            8L,
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TryClaimAsync_MissingClaimStoresValueWithLeaseAndSkipsMemoryCache()
    {
        var (store, cache, locker) = CreateStore();
        FusionCacheEntryOptions? options = null;
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.None));
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Do<FusionCacheEntryOptions>(value => options = value),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask());

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
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.FromValue("existing")));

        Assert.IsFalse(await store.TryClaimAsync(42, "replacement"));
        await cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExecuteIfOwnerAndReplaceAsync_TransfersExactOwnerAndRollsBackWhenActionFails()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.FromValue("lobby-claim")));
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<FusionCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask());

        var result = await store.ExecuteIfOwnerAndReplaceAsync(
            42,
            "lobby-claim",
            "world-claim",
            _ => Task.FromResult(false));

        Assert.IsFalse(result);
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "world-claim",
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "lobby-claim",
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExecuteIfOwnerAndReplaceAsync_WhenActionThrows_RestoresOwnerAndPreservesOriginalFailure()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.FromValue("lobby-claim")));
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<FusionCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask());
        var failure = new InvalidOperationException("local commit failed");

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.ExecuteIfOwnerAndReplaceAsync(
                42,
                "lobby-claim",
                "world-claim",
                _ => Task.FromException<bool>(failure)));

        Assert.AreSame(failure, actual);
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "world-claim",
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "lobby-claim",
            Arg.Any<FusionCacheEntryOptions>(),
            CancellationToken.None);
    }

    [TestMethod]
    public async Task ExecuteIfOwnerAndReplaceAsync_WhenActionIsCanceled_RestoresOwnerAndPreservesCancellation()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.FromValue("lobby-claim")));
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<FusionCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceled = new OperationCanceledException(cancellation.Token);

        var actual = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            store.ExecuteIfOwnerAndReplaceAsync(
                42,
                "lobby-claim",
                "world-claim",
                _ => Task.FromException<bool>(canceled),
                cancellation.Token));

        Assert.AreEqual(cancellation.Token, actual.CancellationToken);
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "lobby-claim",
            Arg.Any<FusionCacheEntryOptions>(),
            CancellationToken.None);
    }

    [TestMethod]
    public async Task ExecuteIfOwnerAndReplaceAsync_WhenCompensationFails_PreservesCallbackFailure()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.FromValue("lobby-claim")));
        var callbackFailure = new InvalidOperationException("local commit failed");
        var compensationFailure = new InvalidOperationException("claim restoration failed");
        var setCalls = 0;
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<FusionCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                setCalls++;
                return setCalls == 1
                    ? new ValueTask()
                    : new ValueTask(Task.FromException(compensationFailure));
            });

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.ExecuteIfOwnerAndReplaceAsync(
                42,
                "lobby-claim",
                "world-claim",
                _ => Task.FromException<bool>(callbackFailure)));

        Assert.AreSame(callbackFailure, actual);
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "lobby-claim",
            Arg.Any<FusionCacheEntryOptions>(),
            CancellationToken.None);
    }

    [TestMethod]
    public async Task ClaimOperation_WhenLockHandleIsMissing_DoesNotInvokeOrRelease()
    {
        var (store, cache, locker) = CreateStore(lockAcquired: false);
        var callbackCalled = false;

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.ExecuteIfOwnerAsync(42, "owner", _ =>
            {
                callbackCalled = true;
                return Task.FromResult(true);
            }));

        Assert.IsFalse(callbackCalled);
        await cache.DidNotReceive().TryGetAsync<string>(
            Arg.Any<string>(),
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        await locker.DidNotReceive().ReleaseLockAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object>(),
            Arg.Any<ILogger>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExecuteIfOwnerAndReplaceAsync_WhenRestorationFailsPropagatesRestorationFailure()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.FromValue("lobby-claim")));
        var setCalls = 0;
        var restorationFailure = new InvalidOperationException("restoration failed");
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<FusionCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                setCalls++;
                return setCalls == 1
                    ? new ValueTask()
                    : new ValueTask(Task.FromException(restorationFailure));
            });

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.ExecuteIfOwnerAndReplaceAsync(
                42,
                "lobby-claim",
                "world-claim",
                _ => Task.FromResult(false)));

        Assert.AreSame(restorationFailure, exception);
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "world-claim",
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        await cache.Received(1).SetAsync(
            "hagalaz:game-session:42",
            "lobby-claim",
            Arg.Any<FusionCacheEntryOptions>(),
            CancellationToken.None);
    }

    [TestMethod]
    public async Task ReleaseAsync_RemovesOnlyExactOwner()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.FromValue("owner")));

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
    public async Task ReleaseAsync_WhenClaimIsAbsent_ReturnsFalseWithoutRemovingAnything()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.None));

        Assert.IsFalse(await store.ReleaseAsync(42, "owner"));
        await cache.DidNotReceive().RemoveAsync(
            Arg.Any<string>(),
            Arg.Any<FusionCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task RenewAsync_RefreshesOnlyExactOwner()
    {
        var (store, cache, _) = CreateStore();
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.FromValue("owner")));

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
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.FromValue("owner")));
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<FusionCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask());
        locker.ReleaseLockAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<ILogger>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask(Task.FromException(new InvalidOperationException("Lock release failed."))));

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
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        cache.TryGetAsync<string>(Arg.Any<string>(), Arg.Any<FusionCacheEntryOptions>(), cancellationToken)
            .Returns(new ValueTask<MaybeValue<string>>(MaybeValue<string>.None));
        cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<FusionCacheEntryOptions>(),
                cancellationToken)
            .Returns(new ValueTask());

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

    private static (FusionCacheGameSessionClaimStore Store, IFusionCache Cache, IFusionCacheDistributedLocker Locker) CreateStore(
        bool lockAcquired = true)
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
            .Returns(new ValueTask<object?>(Task.FromResult<object?>(lockAcquired ? new object() : null)));
        locker.ReleaseLockAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<ILogger>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask());

        return (
            new FusionCacheGameSessionClaimStore(
                cache,
                locker,
                Substitute.For<ILogger<FusionCacheGameSessionClaimStore>>()),
            cache,
            locker);
    }
}

#pragma warning restore CA2012
