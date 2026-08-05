using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;

using Hagalaz.Services.GameWorld.Model;

namespace Hagalaz.Services.GameWorld.Store
{
    public interface IGameSessionStore
    {
        ValueTask<bool> TryAdd(IGameSession session);

        ValueTask<bool> TryReserveWorldSession(IGameWorldSession session);

        ValueTask<(bool Committed, IGameSession? ReplacedSession)> TryCommitWorldSession(IGameWorldSession expectedSession);

        ValueTask<bool> TryRetainWorldSessionForCleanup(IGameSession expectedSession);

        ValueTask<bool> IsPendingWorldSession(IGameSession expectedSession);

        ValueTask<bool> TryRemovePendingWorldSession(IGameSession expectedSession);

        ValueTask<IReadOnlyList<IGameWorldSession>> FindWorldSessionsPendingCleanup();

        ValueTask<bool> TryMoveToPendingAbort(IGameSession expectedSession);

        ValueTask<bool> TryBeginPendingSessionAbort(IGameSession expectedSession);

        ValueTask<bool> TryCompletePendingSessionAbort(IGameSession expectedSession);

        ValueTask<bool> TryReleasePendingSessionAbort(IGameSession expectedSession);

        ValueTask<IReadOnlyList<IGameSession>> FindSessionsPendingAbort();

        ValueTask<(bool Found, IGameSession? Session)> TryGetValue(string connectionId);

        ValueTask<(bool Removed, IGameSession? Session)> TryRemove(IGameSession expectedSession);

        ValueTask<IGameSession?> FindByMasterId(uint masterId);

        ValueTask<IGameWorldSession?> FindWorldSessionByMasterId(uint masterId);

        ValueTask<IReadOnlyList<IGameSession>> FindAll();
    }
}
