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
        var abortCoordinator = new GameSessionAbortCoordinator(
            sessions,
            abortSessions,
            terminator,
            NullLogger<GameSessionAbortCoordinator>.Instance);
        return new GameSessionService(
            sessions,
            factory,
            claims,
            NullLogger<GameSessionService>.Instance,
            abortCoordinator);
    }

    public static GameSessionLeaseService CreateLeaseService(
        IGameSessionStore sessions,
        IGameSessionAbortStore abortSessions,
        IGameSessionClaimStore claims,
        IGameSessionConnectionTerminator terminator)
    {
        var abortCoordinator = new GameSessionAbortCoordinator(
            sessions,
            abortSessions,
            terminator,
            NullLogger<GameSessionAbortCoordinator>.Instance);
        return new GameSessionLeaseService(
            sessions,
            abortSessions,
            claims,
            NullLogger<GameSessionLeaseService>.Instance,
            abortCoordinator);
    }
}
