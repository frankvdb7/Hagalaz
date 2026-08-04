using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Locking.Distributed;

namespace Hagalaz.Services.GameWorld.Services;

public sealed class FusionCacheGameSessionClaimStore : IGameSessionClaimStore
{
    private const string KeyPrefix = "hagalaz:game-session:";
    private const string LockName = "game-session-claim";
    private static readonly TimeSpan DistributedLockTimeout = TimeSpan.FromSeconds(30);
    private static readonly FusionCacheEntryOptions EntryOptions = new()
    {
        Duration = GameSessionClaimOptions.LeaseDuration,
        DistributedCacheDuration = GameSessionClaimOptions.LeaseDuration,
        IsFailSafeEnabled = false,
        SkipMemoryCacheRead = true,
        SkipMemoryCacheWrite = true
    };

    private readonly IFusionCache _cache;
    private readonly IFusionCacheDistributedLocker _locker;
    private readonly ILogger<FusionCacheGameSessionClaimStore> _logger;

    public FusionCacheGameSessionClaimStore(
        IFusionCache cache,
        IFusionCacheDistributedLocker locker,
        ILogger<FusionCacheGameSessionClaimStore> logger)
    {
        _cache = cache;
        _locker = locker;
        _logger = logger;
    }

    public Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) =>
        WithClaimLockAsync(masterId, async token =>
        {
            var current = await _cache.TryGetAsync<string>(GetKey(masterId), EntryOptions, token);
            if (current.HasValue)
            {
                return current.Value == claimId;
            }

            await _cache.SetAsync(GetKey(masterId), claimId, EntryOptions, token);
            return true;
        }, cancellationToken);

    public Task<bool> ReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) =>
        WithClaimLockAsync(masterId, async token =>
        {
            var current = await _cache.TryGetAsync<string>(GetKey(masterId), EntryOptions, token);
            if (!current.HasValue || current.Value != claimId)
            {
                return false;
            }

            await _cache.RemoveAsync(GetKey(masterId), EntryOptions, token);
            return true;
        }, cancellationToken);

    public Task<bool> RenewAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) =>
        WithClaimLockAsync(masterId, async token =>
        {
            var current = await _cache.TryGetAsync<string>(GetKey(masterId), EntryOptions, token);
            if (!current.HasValue || current.Value != claimId)
            {
                return false;
            }

            await _cache.SetAsync(GetKey(masterId), claimId, EntryOptions, token);
            return true;
        }, cancellationToken);

    public Task<bool> ExecuteIfOwnerAsync(
        uint masterId,
        string claimId,
        Func<CancellationToken, Task<bool>> action,
        CancellationToken cancellationToken = default) =>
        WithClaimLockAsync(masterId, async token =>
        {
            var current = await _cache.TryGetAsync<string>(GetKey(masterId), EntryOptions, token);
            if (!current.HasValue || current.Value != claimId)
            {
                return false;
            }

            await _cache.SetAsync(GetKey(masterId), claimId, EntryOptions, token);
            return await action(token);
        }, cancellationToken);

    private async Task<T> WithClaimLockAsync<T>(uint masterId, Func<CancellationToken, Task<T>> action, CancellationToken token)
    {
        var key = GetKey(masterId);
        var operationId = Guid.NewGuid().ToString("N");
        var lockObject = await _locker.AcquireLockAsync(
            _cache.CacheName,
            _cache.InstanceId,
            operationId,
            key,
            LockName,
            DistributedLockTimeout,
            _logger,
            token);

        try
        {
            return await action(token);
        }
        finally
        {
            try
            {
                await _locker.ReleaseLockAsync(
                    _cache.CacheName,
                    _cache.InstanceId,
                    operationId,
                    key,
                    LockName,
                    lockObject,
                    _logger,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to release distributed game-session claim lock for account '{masterId}'.",
                    masterId);
            }
        }
    }

    private static string GetKey(uint masterId) => $"{KeyPrefix}{masterId}";
}
