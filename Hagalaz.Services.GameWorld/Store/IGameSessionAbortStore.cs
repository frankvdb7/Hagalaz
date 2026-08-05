using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Store;

public interface IGameSessionAbortStore
{
    ValueTask<bool> TryMoveToPendingAbort(IGameSession expectedSession);

    ValueTask<bool> TryBeginPendingSessionAbort(IGameSession expectedSession);

    ValueTask<bool> TryCompletePendingSessionAbort(IGameSession expectedSession);

    ValueTask<bool> TryReleasePendingSessionAbort(IGameSession expectedSession);

    ValueTask<IReadOnlyList<IGameSession>> FindSessionsPendingAbort();
}
