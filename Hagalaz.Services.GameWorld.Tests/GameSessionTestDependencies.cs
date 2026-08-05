using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hagalaz.Services.GameWorld.Tests;

internal static class GameSessionTestDependencies
{
    public static GameSessionService CreateService(
        IGameSessionStore sessions,
        IGameSessionAbortStore abortSessions,
        IGameSessionFactory factory,
        IGameSessionClaimStore claims,
        IGameSessionConnectionTerminator terminator)
    {
        var retryQueue = new GameSessionRetryQueue(
            claims,
            terminator,
            NullLogger<GameSessionRetryQueue>.Instance,
            abortSessions);
        return new GameSessionService(
            sessions,
            abortSessions,
            factory,
            claims,
            terminator,
            NullLogger<GameSessionService>.Instance,
            retryQueue);
    }

    public static GameSessionLeaseService CreateLeaseService(
        IGameSessionStore sessions,
        IGameSessionAbortStore abortSessions,
        IGameSessionClaimStore claims,
        IGameSessionConnectionTerminator terminator)
    {
        var retryQueue = new GameSessionRetryQueue(
            claims,
            terminator,
            NullLogger<GameSessionRetryQueue>.Instance,
            abortSessions);
        return new GameSessionLeaseService(
            sessions,
            abortSessions,
            claims,
            terminator,
            NullLogger<GameSessionLeaseService>.Instance,
            retryQueue);
    }
}
