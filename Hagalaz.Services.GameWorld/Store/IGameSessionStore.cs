using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;

using Hagalaz.Services.GameWorld.Model;

namespace Hagalaz.Services.GameWorld.Store
{
    public interface IGameSessionStore : IGameSessionAbortStore
    {
        ValueTask<bool> TryAdd(IGameSession session);

        ValueTask<bool> TryReserveWorldSession(IGameWorldSession session);

        ValueTask<(bool Committed, IGameSession? ReplacedSession)> TryCommitWorldSession(IGameWorldSession expectedSession);

        ValueTask<bool> TryRetainWorldSessionForCleanup(IGameSession expectedSession);

        ValueTask<bool> IsPendingWorldSession(IGameSession expectedSession);

        ValueTask<bool> TryRemovePendingWorldSession(IGameSession expectedSession);

        ValueTask<IReadOnlyList<IGameWorldSession>> FindWorldSessionsPendingCleanup();

        ValueTask<(bool Found, IGameSession? Session)> TryGetValue(string connectionId);

        ValueTask<(bool Removed, IGameSession? Session)> TryRemove(IGameSession expectedSession);

        ValueTask<IGameSession?> FindByMasterId(uint masterId);

        ValueTask<IGameWorldSession?> FindWorldSessionByMasterId(uint masterId);

        ValueTask<IReadOnlyList<IGameSession>> FindAll();
    }
}
