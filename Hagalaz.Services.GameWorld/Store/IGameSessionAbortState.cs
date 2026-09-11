using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Store;

/// <summary>
/// Exposes the atomic local state transitions used to reconcile connection aborts.
/// The implementation is the same singleton as <see cref="IGameSessionStore"/>.
/// </summary>
public interface IGameSessionAbortState
{
    ValueTask<bool> TryMoveToPendingAbort(IGameSession expectedSession);

    ValueTask<long?> TryBeginPendingSessionAbort(IGameSession expectedSession);

    ValueTask<bool> TryCompletePendingSessionAbort(IGameSession expectedSession, long processingToken);

    ValueTask<bool> TryReleasePendingSessionAbort(IGameSession expectedSession, long processingToken);

    ValueTask<IReadOnlyList<IGameSession>> FindSessionsPendingAbort();
}
