using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Services.GameWorld.Services;

public static class GameSessionClaimOptions
{
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan RenewalInterval = TimeSpan.FromSeconds(30);
}

public interface IGameSessionClaimStore
{
    Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default);
    Task<bool> ReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default);
    Task<bool> RenewAsync(uint masterId, string claimId, CancellationToken cancellationToken = default);
    Task<bool> ExecuteIfOwnerAsync(
        uint masterId,
        string claimId,
        Func<CancellationToken, Task<bool>> action,
        CancellationToken cancellationToken = default);
}
