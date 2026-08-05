using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Network.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class GameSessionServiceTests
{
    [TestMethod]
    public void GameSessionFactory_CreateLobbySession_IsNotWorldSession()
    {
        var factory = new GameSessionFactory(Substitute.For<Raido.Server.IRaidoLifetimeManager>());

        var lobbySession = factory.Create(42, "lobby-connection");
        var worldSession = factory.CreateWorld(42, "world-connection");

        Assert.IsFalse(lobbySession is IGameWorldSession);
        Assert.IsInstanceOfType<IGameWorldSession>(worldSession);
    }

    [TestMethod]
    public async Task GameSessionStore_PendingAbortBlocksConnectionIdReuse()
    {
        var store = new GameSessionStore();
        var retainedSession = CreateLobbySession(42, "reused-connection");
        var replacementLobbySession = CreateLobbySession(43, "reused-connection");
        var replacementWorldSession = CreateSession(43, "reused-connection", "replacement-claim");

        Assert.IsTrue(await store.TryAdd(retainedSession));
        Assert.IsTrue(await store.TryMoveToPendingAbort(retainedSession));

        Assert.IsFalse(await store.TryAdd(replacementLobbySession));
        Assert.IsFalse(await store.TryReserveWorldSession(replacementWorldSession));
        Assert.IsFalse(await store.TryMoveToPendingAbort(replacementLobbySession));
        Assert.AreEqual(1, (await store.FindSessionsPendingAbort()).Count);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task GameSessionStore_MoveToPendingAbort_IsAtomicWithConnectionIdReuse()
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var store = new GameSessionStore();
            var retainedSession = CreateLobbySession(42, $"reused-connection-{attempt}");
            var replacementSession = CreateLobbySession(43, retainedSession.ConnectionId);
            Assert.IsTrue(await store.TryAdd(retainedSession));

            using var startGate = new Barrier(2);
            var moveTask = Task.Run(async () =>
            {
                startGate.SignalAndWait();
                return await store.TryMoveToPendingAbort(retainedSession);
            });
            var addTask = Task.Run(async () =>
            {
                startGate.SignalAndWait();
                return await store.TryAdd(replacementSession);
            });

            await Task.WhenAll(moveTask, addTask);

            Assert.IsTrue(moveTask.Result);
            Assert.IsFalse(addTask.Result);
            Assert.AreEqual(1, (await store.FindSessionsPendingAbort()).Count);
        }
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task GameSessionStore_PendingAbortProcessing_AllowsOnlyOneCoordinatorToClaimReservation()
    {
        var store = new GameSessionStore();
        var session = CreateLobbySession(42, "pending-abort-connection");

        Assert.IsTrue(await store.TryAdd(session));
        Assert.IsTrue(await store.TryMoveToPendingAbort(session));

        using var startGate = new Barrier(2);
        var firstTask = Task.Run(async () =>
        {
            startGate.SignalAndWait();
            return await store.TryBeginPendingSessionAbort(session);
        });
        var secondTask = Task.Run(async () =>
        {
            startGate.SignalAndWait();
            return await store.TryBeginPendingSessionAbort(session);
        });

        await Task.WhenAll(firstTask, secondTask);

        Assert.AreNotEqual(firstTask.Result.Began, secondTask.Result.Began);
        Assert.IsTrue(firstTask.Result.Began || secondTask.Result.Began);
        var processingLease = firstTask.Result.Began ? firstTask.Result : secondTask.Result;
        Assert.IsTrue(await store.TryCompletePendingSessionAbort(session, processingLease.ProcessingToken));
        Assert.AreEqual(0, (await store.FindSessionsPendingAbort()).Count);
    }

    [TestMethod]
    public async Task GameSessionStore_ExpiredAbortProcessingLease_CanBeReclaimedWithoutStaleCompletion()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var store = new GameSessionStore(timeProvider);
        var session = CreateLobbySession(42, "expired-abort-connection");

        Assert.IsTrue(await store.TryAdd(session));
        Assert.IsTrue(await store.TryMoveToPendingAbort(session));

        var firstLease = await store.TryBeginPendingSessionAbort(session);
        Assert.IsTrue(firstLease.Began);

        timeProvider.Advance(GameSessionAbortOptions.ProcessingTimeout + TimeSpan.FromSeconds(1));

        Assert.AreEqual(1, (await store.FindSessionsPendingAbort()).Count);
        var secondLease = await store.TryBeginPendingSessionAbort(session);
        Assert.IsTrue(secondLease.Began);
        Assert.AreNotEqual(firstLease.ProcessingToken, secondLease.ProcessingToken);
        Assert.IsFalse(await store.TryCompletePendingSessionAbort(session, firstLease.ProcessingToken));
        Assert.IsTrue(await store.TryCompletePendingSessionAbort(session, secondLease.ProcessingToken));
        Assert.AreEqual(0, (await store.FindSessionsPendingAbort()).Count);
    }

    [TestMethod]
    public async Task AbortCoordinator_WhenCompletionFails_ReleasesProcessingMarker()
    {
        var store = new GameSessionStore();
        var session = CreateLobbySession(42, "abort-connection");
        Assert.IsTrue(await store.TryAdd(session));
        Assert.IsTrue(await store.TryMoveToPendingAbort(session));

        var abortStore = new CompletionFailingAbortStore(store);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var coordinator = new GameSessionAbortCoordinator(
            store,
            abortStore,
            terminator,
            NullLogger<GameSessionAbortCoordinator>.Instance);

        Assert.IsFalse(await coordinator.AbortPendingSessionAsync(session, CancellationToken.None));
        Assert.AreEqual(1, abortStore.ReleaseCalls);
        Assert.AreEqual(1, (await store.FindSessionsPendingAbort()).Count);

        Assert.IsTrue(await coordinator.AbortPendingSessionAsync(session, CancellationToken.None));
        Assert.AreEqual(0, (await store.FindSessionsPendingAbort()).Count);
        terminator.Received(2).Abort(session);
    }

    [TestMethod]
    public async Task TryAddWorldSession_ConcurrentWorlds_OnlyOneClaimsAccount()
    {
        var claims = new BarrierGameSessionClaimStore();
        var first = CreateService(claims, 42, "connection-1", "claim-1");
        var second = CreateService(claims, 42, "connection-2", "claim-2");

        var registrationsTask = Task.WhenAll(
            first.TryAddWorldSession(42, "connection-1"),
            second.TryAddWorldSession(42, "connection-2"));
        await claims.WaitForBothClaimAttemptsAsync();
        claims.ReleaseClaimAttempts();
        var registrations = await registrationsTask;

        Assert.AreEqual(1, registrations.Count(registration => registration.Created));
        Assert.AreEqual(1, claims.Count);
        Assert.AreEqual(2, claims.TryClaimAttempts);
    }

    [TestMethod]
    public async Task TryAddWorldSession_DifferentAccounts_CanClaimConcurrently()
    {
        var claims = new BarrierGameSessionClaimStore();
        var first = CreateService(claims, 42, "connection-1", "claim-1");
        var second = CreateService(claims, 43, "connection-2", "claim-2");

        var registrationsTask = Task.WhenAll(
            first.TryAddWorldSession(42, "connection-1"),
            second.TryAddWorldSession(43, "connection-2"));
        await claims.WaitForBothClaimAttemptsAsync();
        claims.ReleaseClaimAttempts();
        var registrations = await registrationsTask;

        Assert.IsTrue(registrations[0].Created);
        Assert.IsTrue(registrations[1].Created);
        Assert.AreEqual(2, claims.Count);
        Assert.AreEqual(2, claims.TryClaimAttempts);
    }

    [TestMethod]
    public async Task TryAddWorldSession_WithClaimlessLobbySession_StillAttemptsWorldClaim()
    {
        var claims = Substitute.For<IGameSessionClaimStore>();
        claims.TryClaimAsync(42, "world-claim", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        var factory = Substitute.For<IGameSessionFactory>();
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var worldSession = CreateSession(42, "world-connection", "world-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "world-connection").Returns(worldSession);
        var store = new GameSessionStore();
        var service = GameSessionTestDependencies.CreateService(
            store, store, factory, claims, Substitute.For<IGameSessionConnectionTerminator>());

        Assert.IsTrue((await service.AddSession(42, "lobby-connection")).Created);

        var registration = await service.TryAddWorldSession(42, "world-connection");

        Assert.IsTrue(registration.Created);
        Assert.AreSame(lobbySession, await service.FindByMasterId(42));
        await claims.Received(1).TryClaimAsync(42, "world-claim", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task TryAddWorldSession_SameWorldAttemptsAreSerializedByClaim()
    {
        var claims = new BarrierGameSessionClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var firstSession = CreateSession(42, "connection-1", "claim-1");
        var secondSession = CreateSession(42, "connection-2", "claim-2");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "connection-1").Returns(firstSession);
        factory.CreateWorld(42, "connection-2").Returns(secondSession);
        var store = new GameSessionStore();
        var service = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
        await service.AddSession(42, "lobby-connection");

        var registrationsTask = Task.WhenAll(
            service.TryAddWorldSession(42, "connection-1"),
            service.TryAddWorldSession(42, "connection-2"));
        await claims.WaitForBothClaimAttemptsAsync();
        claims.ReleaseClaimAttempts();
        var registrations = await registrationsTask;

        Assert.AreEqual(1, registrations.Count(registration => registration.Created));
        Assert.AreEqual(1, claims.Count);
        Assert.AreEqual(2, claims.TryClaimAttempts);
        await service.CommitWorldSession(registrations.Single(registration => registration.Created).Session!);
        terminator.Received(1).Abort(lobbySession);
        var activeSession = await service.FindByMasterId(42);
        Assert.IsNotNull(activeSession);
    }

    [TestMethod]
    public async Task CommitWorldSession_SameConnectionDoesNotAbortPromotedSession()
    {
        var claims = new InMemoryGameSessionClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var lobbySession = CreateLobbySession(42, "shared-connection");
        var worldSession = CreateSession(42, "shared-connection", "world-claim");
        factory.Create(42, "shared-connection").Returns(lobbySession);
        factory.CreateWorld(42, "shared-connection").Returns(worldSession);
        var store = new GameSessionStore();
        var service = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);

        var lobbyRegistration = await service.AddSession(42, "shared-connection");
        var worldRegistration = await service.TryAddWorldSession(42, "shared-connection");

        Assert.IsTrue(lobbyRegistration.Created);
        Assert.IsTrue(worldRegistration.Created);
        Assert.IsTrue(await service.CommitWorldSession(worldSession));
        terminator.DidNotReceive().Abort(Arg.Any<IGameSession>());
        Assert.AreSame(worldSession, await service.FindByMasterId(42));
    }

    [TestMethod]
    public async Task CommitWorldSession_WhenClaimExpiresBeforeCommit_DoesNotPromoteSession()
    {
        var store = new GameSessionStore();
        var claims = new InMemoryGameSessionClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var session = CreateSession(42, "world-connection", "world-claim");
        factory.CreateWorld(42, "world-connection").Returns(session);
        var service = GameSessionTestDependencies.CreateService(
            store, store, factory, claims, Substitute.For<IGameSessionConnectionTerminator>());

        var registration = await service.TryAddWorldSession(42, "world-connection");
        Assert.IsTrue(registration.Created);

        claims.Replace(42, "other-world-claim");

        Assert.IsFalse(await service.CommitWorldSession(session));
        Assert.IsNull(await service.FindByMasterId(42));
        Assert.AreEqual(1, (await store.FindAll()).Count);

        Assert.IsFalse(await service.RemoveSession(session));
        Assert.AreEqual(1, (await store.FindAll()).Count);
        Assert.AreEqual(1, (await store.FindWorldSessionsPendingCleanup()).Count);
    }

    [TestMethod]
    public async Task CommitWorldSession_WhenExactClaimReleaseThrows_RetainsPendingCleanup()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        var factory = Substitute.For<IGameSessionFactory>();
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var staleWorldSession = CreateSession(42, "world-connection", "world-claim");
        var winningWorldSession = CreateSession(42, "winning-world-connection", "winning-world-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "world-connection").Returns(staleWorldSession);
        factory.CreateWorld(42, "winning-world-connection").Returns(winningWorldSession);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var abortCoordinator = CreateAbortCoordinator(store, terminator);
        var service = new GameSessionService(store, factory, claims, NullLogger<GameSessionService>.Instance, abortCoordinator);
        claims.TryClaimAsync(42, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        await service.AddSession(42, "lobby-connection");
        var staleRegistration = await service.TryAddWorldSession(42, "world-connection");
        var winningRegistration = await service.TryAddWorldSession(42, "winning-world-connection");
        Assert.IsTrue(staleRegistration.Created);
        Assert.IsTrue(winningRegistration.Created);
        claims.ExecuteIfOwnerAsync(
                Arg.Any<uint>(),
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<bool>>>()!(CancellationToken.None));
        var releaseFailure = new InvalidOperationException("Claim store unavailable.");
        claims.ReleaseAsync(42, "world-claim", CancellationToken.None)
            .Returns(Task.FromException<bool>(releaseFailure));

        Assert.IsTrue(await service.CommitWorldSession(winningWorldSession));
        Assert.IsFalse(await service.CommitWorldSession(staleWorldSession));
        Assert.AreSame(winningWorldSession, await service.FindByMasterId(42));
        Assert.AreEqual(2, (await store.FindAll()).Count);
        Assert.AreEqual(1, (await store.FindWorldSessionsPendingCleanup()).Count);
        await claims.Received(1).ReleaseAsync(42, "world-claim", CancellationToken.None);
    }

    [TestMethod]
    public async Task CommitWorldSession_WhenClaimReleaseThrows_RetainsExactOwnerCleanup()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        var factory = Substitute.For<IGameSessionFactory>();
        var session = CreateSession(42, "world-connection", "world-claim");
        factory.CreateWorld(42, "world-connection").Returns(session);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var abortCoordinator = CreateAbortCoordinator(store, terminator);
        var service = new GameSessionService(store, factory, claims, NullLogger<GameSessionService>.Instance, abortCoordinator);
        claims.TryClaimAsync(42, "world-claim", Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var releaseFailure = new InvalidOperationException("Claim store unavailable.");
        claims.ReleaseAsync(42, "world-claim", CancellationToken.None)
            .Returns(Task.FromException<bool>(releaseFailure));
        await service.TryAddWorldSession(42, "world-connection");
        claims.ExecuteIfOwnerAsync(
                42,
                "world-claim",
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        Assert.IsFalse(await service.CommitWorldSession(session));
        Assert.AreEqual(1, (await store.FindWorldSessionsPendingCleanup()).Count);
        Assert.AreEqual(1, (await store.FindAll()).Count);
    }

    [TestMethod]
    public async Task RemoveSession_WhenClaimReleaseThrows_RetainsExactOwnerCleanup()
    {
        var claims = Substitute.For<IGameSessionClaimStore>();
        var factory = Substitute.For<IGameSessionFactory>();
        var session = CreateSession(42, "world-connection", "world-claim");
        factory.CreateWorld(42, "world-connection").Returns(session);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var store = new GameSessionStore();
        var abortCoordinator = CreateAbortCoordinator(store, terminator);
        var service = new GameSessionService(store, factory, claims, NullLogger<GameSessionService>.Instance, abortCoordinator);
        claims.TryClaimAsync(42, "world-claim", Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        Assert.IsTrue((await service.TryAddWorldSession(42, "world-connection")).Created);
        claims.ExecuteIfOwnerAsync(
                42,
                "world-claim",
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<bool>>>()!(CancellationToken.None));
        Assert.IsTrue(await service.CommitWorldSession(session));
        claims.ReleaseAsync(42, "world-claim", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("Claim store unavailable.")));

        Assert.IsTrue(await service.RemoveSession(session));
        Assert.AreEqual(1, (await store.FindWorldSessionsPendingCleanup()).Count);
        Assert.IsNull(await service.FindByMasterId(42));
    }

    [TestMethod]
    public async Task CommitWorldSession_WhenReplacingAbortThrows_StillCommitsWorldSession()
    {
        var claims = new InMemoryGameSessionClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var terminator = new FailOnceConnectionTerminator();
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var worldSession = CreateSession(42, "world-connection", "world-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "world-connection").Returns(worldSession);
        var store = new GameSessionStore();
        var abortCoordinator = CreateAbortCoordinator(store, terminator);
        var service = new GameSessionService(store, factory, claims, NullLogger<GameSessionService>.Instance, abortCoordinator);
        await service.AddSession(42, "lobby-connection");
        var registration = await service.TryAddWorldSession(42, "world-connection");

        Assert.IsTrue(await service.CommitWorldSession(registration.Session!));
        Assert.AreSame(worldSession, await service.FindByMasterId(42));
        Assert.AreEqual(1, terminator.Attempts);
        Assert.AreEqual(1, (await store.FindSessionsPendingAbort()).Count);

        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(CancellationToken.None);

        Assert.AreEqual(2, terminator.Attempts);
        Assert.AreSame(lobbySession, terminator.LastAbortedSession);
        Assert.AreEqual(0, (await store.FindSessionsPendingAbort()).Count);
        Assert.IsTrue(await store.TryAdd(CreateLobbySession(43, lobbySession.ConnectionId)));
    }

    [TestMethod]
    public async Task CommitWorldSession_WhenAbortFails_LeaseReconciliationClearsReservation()
    {
        var store = new GameSessionStore();
        var claims = new InMemoryGameSessionClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var terminator = new FailOnceConnectionTerminator();
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var worldSession = CreateSession(42, "world-connection", "world-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "world-connection").Returns(worldSession);
        var abortCoordinator = CreateAbortCoordinator(store, terminator);
        var service = new GameSessionService(store, factory, claims, NullLogger<GameSessionService>.Instance, abortCoordinator);

        await service.AddSession(42, "lobby-connection");
        var registration = await service.TryAddWorldSession(42, "world-connection");

        Assert.IsTrue(await service.CommitWorldSession(registration.Session!));
        Assert.AreEqual(1, (await store.FindSessionsPendingAbort()).Count);
        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(CancellationToken.None);
        Assert.AreSame(lobbySession, terminator.LastAbortedSession);
    }

    [TestMethod]
    public async Task CommitWorldSession_WhenAbortCancelsAfterPromotion_KeepsPromotedSession()
    {
        using var cancellationSource = new CancellationTokenSource();
        var store = new GameSessionStore();
        var claims = new InMemoryGameSessionClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var terminator = new CancelOnceConnectionTerminator(cancellationSource);
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var worldSession = CreateSession(42, "world-connection", "world-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "world-connection").Returns(worldSession);
        var service = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);

        await service.AddSession(42, "lobby-connection");
        var registration = await service.TryAddWorldSession(42, "world-connection");

        var committed = await service.CommitWorldSession(registration.Session!, cancellationSource.Token);

        Assert.IsTrue(committed);
        Assert.AreSame(worldSession, await service.FindByMasterId(42));
        Assert.AreEqual(1, (await store.FindSessionsPendingAbort()).Count);

        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(CancellationToken.None);

        Assert.AreEqual(2, terminator.Attempts);
        Assert.AreSame(lobbySession, terminator.LastAbortedSession);
        Assert.AreEqual(0, (await store.FindSessionsPendingAbort()).Count);
    }

    [TestMethod]
    public async Task GameSessionStore_WhenAnotherWorldPromotesFirst_RejectsStalePendingCommit()
    {
        var store = new GameSessionStore();
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var firstWorldSession = CreateSession(42, "world-connection-1", "world-claim-1");
        var secondWorldSession = CreateSession(42, "world-connection-2", "world-claim-2");

        Assert.IsTrue(await store.TryAdd(lobbySession));
        Assert.IsTrue(await store.TryReserveWorldSession(firstWorldSession));
        Assert.IsTrue(await store.TryReserveWorldSession(secondWorldSession));

        var firstCommit = await store.TryCommitWorldSession(firstWorldSession);
        Assert.IsTrue(firstCommit.Committed);
        Assert.AreSame(lobbySession, firstCommit.ReplacedSession);

        var staleCommit = await store.TryCommitWorldSession(secondWorldSession);
        Assert.IsFalse(staleCommit.Committed);
        Assert.IsNull(staleCommit.ReplacedSession);
        Assert.AreSame(firstWorldSession, await store.FindByMasterId(42));
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task TryAddWorldSession_WhenClaimCleanupFails_RetainsForLeaseReconciliation()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        claims.TryClaimAsync(42, "world-claim", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("Claim store unavailable.")));
        var releaseAttempts = 0;
        claims.ReleaseAsync(42, "world-claim", Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref releaseAttempts) == 1
                ? Task.FromException<bool>(new InvalidOperationException("Claim store unavailable."))
                : Task.FromResult(true));
        var factory = Substitute.For<IGameSessionFactory>();
        var worldSession = CreateSession(42, "world-connection", "world-claim");
        factory.CreateWorld(42, "world-connection").Returns(worldSession);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var service = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.TryAddWorldSession(42, "world-connection"));

        Assert.AreEqual(1, (await store.FindAll()).Count);
        Assert.IsNull(await store.FindWorldSessionByMasterId(42));

        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(CancellationToken.None);

        Assert.AreEqual(2, releaseAttempts);
        Assert.AreEqual(0, (await store.FindAll()).Count);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task LeaseService_RenewsActiveSessionBeforeBlockedDeferredCleanup()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var activeSession = CreateSession(42, "active-connection", "active-claim");
        var deferredSession = CreateSession(43, "deferred-connection", "deferred-claim");
        Assert.IsTrue(await store.TryAdd(activeSession));
        Assert.IsTrue(await store.TryReserveWorldSession(deferredSession));
        Assert.IsTrue(await store.TryRetainWorldSessionForCleanup(deferredSession));

        var activeRenewed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseDeferred = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        claims.RenewAsync(42, "active-claim", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                activeRenewed.TrySetResult(true);
                return Task.FromResult(true);
            });
        claims.ReleaseAsync(43, "deferred-claim", Arg.Any<CancellationToken>())
            .Returns(_ => releaseDeferred.Task);
        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);

        var renewalTask = leaseService.RenewSessionsAsync(CancellationToken.None);
        await activeRenewed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsFalse(renewalTask.IsCompleted);

        releaseDeferred.TrySetResult(true);
        await renewalTask;
        await claims.Received(1).RenewAsync(42, "active-claim", Arg.Any<CancellationToken>());
        Assert.AreEqual(0, (await store.FindWorldSessionsPendingCleanup()).Count);
    }

    [TestMethod]
    public async Task TryAddWorldSession_WhenClaimAcquisitionThrows_PreservesLobbySession()
    {
        var claims = Substitute.For<IGameSessionClaimStore>();
        var factory = Substitute.For<IGameSessionFactory>();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var worldSession = CreateSession(42, "world-connection", "world-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "world-connection").Returns(worldSession);
        var store = new GameSessionStore();
        var service = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
        await service.AddSession(42, "lobby-connection");
        var claimFailure = new InvalidOperationException("Redis is unavailable.");
        claims.TryClaimAsync(42, "world-claim").Returns(Task.FromException<bool>(claimFailure));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.TryAddWorldSession(42, "world-connection"));

        Assert.AreSame(claimFailure, exception);
        Assert.AreSame(lobbySession, await service.FindByMasterId(42));
        terminator.DidNotReceive().Abort(Arg.Any<IGameSession>());
    }

    [TestMethod]
    public async Task TryAddWorldSession_WhenClaimPersistsBeforeAcquisitionThrows_ReleasesClaimForLaterLogin()
    {
        var claims = new PersistThenThrowGameSessionClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var failedWorldSession = CreateSession(42, "failed-world-connection", "failed-claim");
        var laterWorldSession = CreateSession(42, "later-world-connection", "later-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "failed-world-connection").Returns(failedWorldSession);
        factory.CreateWorld(42, "later-world-connection").Returns(laterWorldSession);
        var store = new GameSessionStore();
        var service = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
        await service.AddSession(42, "lobby-connection");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.TryAddWorldSession(42, "failed-world-connection"));

        Assert.AreSame(lobbySession, await service.FindByMasterId(42));
        Assert.AreEqual(1, claims.ReleaseCalls);
        Assert.IsNull(claims.CurrentClaim);

        var laterRegistration = await service.TryAddWorldSession(42, "later-world-connection");

        Assert.IsTrue(laterRegistration.Created);
        Assert.AreSame(laterWorldSession, laterRegistration.Session);
        terminator.DidNotReceive().Abort(Arg.Any<IGameSession>());
    }

    [TestMethod]
    public async Task TryAddWorldSession_WhenClaimReleaseFails_RetainsExactOwnerCleanup()
    {
        var store = new GameSessionStore();
        var claims = new PersistThenThrowReleaseGameSessionClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var failedWorldSession = CreateSession(42, "failed-world-connection", "failed-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "failed-world-connection").Returns(failedWorldSession);
        var service = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
        await service.AddSession(42, "lobby-connection");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.TryAddWorldSession(42, "failed-world-connection"));

        Assert.AreEqual(1, claims.ReleaseCalls);
        Assert.AreEqual(1, (await store.FindWorldSessionsPendingCleanup()).Count);
        Assert.AreSame(lobbySession, await service.FindByMasterId(42));
        Assert.AreEqual(2, (await store.FindAll()).Count);

        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(CancellationToken.None);

        Assert.AreEqual(2, claims.ReleaseCalls);
        Assert.IsNull(claims.CurrentClaim);
        Assert.AreEqual(0, (await store.FindWorldSessionsPendingCleanup()).Count);
    }

    [TestMethod]
    public async Task TryAddWorldSession_WhenCancellationOccursAfterClaimPersistence_ReleasesClaimForLaterLogin()
    {
        using var cancellationSource = new CancellationTokenSource();
        var claims = new CancelAfterPersistGameSessionClaimStore(cancellationSource);
        var factory = Substitute.For<IGameSessionFactory>();
        var lobbySession = CreateLobbySession(42, "lobby-connection");
        var failedWorldSession = CreateSession(42, "failed-world-connection", "failed-claim");
        var laterWorldSession = CreateSession(42, "later-world-connection", "later-claim");
        factory.Create(42, "lobby-connection").Returns(lobbySession);
        factory.CreateWorld(42, "failed-world-connection").Returns(failedWorldSession);
        factory.CreateWorld(42, "later-world-connection").Returns(laterWorldSession);
        var store = new GameSessionStore();
        var service = GameSessionTestDependencies.CreateService(
            store, store, factory, claims, Substitute.For<IGameSessionConnectionTerminator>());
        await service.AddSession(42, "lobby-connection");

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => service.TryAddWorldSession(42, "failed-world-connection", cancellationSource.Token));

        Assert.AreSame(lobbySession, await service.FindByMasterId(42));
        Assert.AreEqual(1, claims.ReleaseCalls);
        Assert.IsNull(claims.CurrentClaim);

        var laterRegistration = await service.TryAddWorldSession(42, "later-world-connection");

        Assert.IsTrue(laterRegistration.Created);
        Assert.AreSame(laterWorldSession, laterRegistration.Session);
    }

    [TestMethod]
    public async Task RemoveSession_StaleClaimRetainsSessionForReconciliation()
    {
        var claims = new InMemoryGameSessionClaimStore();
        var service = CreateService(claims, 42, "connection-1", "claim-1");
        var registration = await service.TryAddWorldSession(42, "connection-1");
        Assert.IsTrue(await service.CommitWorldSession(registration.Session!));

        claims.Replace(42, "claim-2");

        Assert.IsTrue(await service.RemoveSession(registration.Session!));
        Assert.AreEqual("claim-2", claims.Get(42));
        Assert.IsNull(await service.FindByMasterId(42));
    }

    [TestMethod]
    public async Task RemoveSession_WhenClaimReleaseFails_RetainsSessionForLeaseReconciliation()
    {
        var claims = new InMemoryGameSessionClaimStore();
        var service = CreateService(claims, 42, "connection-1", "claim-1");
        var registration = await service.TryAddWorldSession(42, "connection-1");
        Assert.IsTrue(await service.CommitWorldSession(registration.Session!));

        claims.Replace(42, "claim-2");

        Assert.IsTrue(await service.RemoveSession(registration.Session!));
        Assert.IsNull(await service.FindByMasterId(42));
    }

    [TestMethod]
    public async Task RemoveSession_ExpectedSessionCannotReleaseReplacementAtSameConnection()
    {
        var store = new GameSessionStore();
        var claims = new InMemoryGameSessionClaimStore();
        var expectedSession = CreateSession(42, "connection", "claim-1");
        var replacementSession = CreateSession(42, "connection", "claim-1");
        Assert.IsTrue(await store.TryAdd(replacementSession));
        claims.Replace(42, "claim-1");
        var service = GameSessionTestDependencies.CreateService(
            store,
            store,
            Substitute.For<IGameSessionFactory>(),
            claims,
            Substitute.For<IGameSessionConnectionTerminator>());

        Assert.IsFalse(await service.RemoveSession(expectedSession));
        Assert.AreEqual("claim-1", claims.Get(42));
        Assert.AreSame(replacementSession, await service.FindByMasterId(42));
    }

    [TestMethod]
    public async Task AddSession_LocalStoreRejectsDuplicateMasterId()
    {
        var claims = new InMemoryGameSessionClaimStore();
        var factory = Substitute.For<IGameSessionFactory>();
        var firstSession = CreateLobbySession(42, "connection-1");
        var secondSession = CreateLobbySession(42, "connection-2");
        factory.Create(42, "connection-1").Returns(firstSession);
        factory.Create(42, "connection-2").Returns(secondSession);
        var store = new GameSessionStore();
        var service = GameSessionTestDependencies.CreateService(
            store, store, factory, claims, Substitute.For<IGameSessionConnectionTerminator>());

        var first = await service.AddSession(42, "connection-1");
        var second = await service.AddSession(42, "connection-2");

        Assert.IsTrue(first.Created);
        Assert.IsFalse(second.Created);
        Assert.AreSame(firstSession, second.Session);
    }

    [TestMethod]
    public async Task LeaseService_HealthyClaimIsRenewedWithoutAbortingConnection()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var session = CreateSession(42, "connection", "claim");
        var factory = Substitute.For<IGameSessionFactory>();
        factory.CreateWorld(42, "connection").Returns(session);
        var gameSessions = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
        claims.TryClaimAsync(42, "claim").Returns(Task.FromResult(true));
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        await gameSessions.TryAddWorldSession(42, "connection");
        claims.RenewAsync(42, "claim", cancellationToken).Returns(Task.FromResult(true));

        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(cancellationToken);

        await claims.Received(1).RenewAsync(42, "claim", cancellationToken);
        terminator.DidNotReceive().Abort(Arg.Any<IGameSession>());
    }

    [TestMethod]
    public async Task LeaseService_LostClaimAbortsOldConnection()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var session = CreateSession(42, "connection", "claim");
        var factory = Substitute.For<IGameSessionFactory>();
        factory.CreateWorld(42, "connection").Returns(session);
        var gameSessions = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
        claims.TryClaimAsync(42, "claim").Returns(Task.FromResult(true));
        await gameSessions.TryAddWorldSession(42, "connection");
        claims.RenewAsync(42, "claim").Returns(Task.FromResult(false));

        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(CancellationToken.None);

        terminator.Received(1).Abort(session);
        Assert.IsNull(await gameSessions.FindByMasterId(42));
    }

    [TestMethod]
    public async Task LeaseService_RenewalExceptionReconcilesLocalSession()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var session = CreateSession(42, "connection", "claim");
        var factory = Substitute.For<IGameSessionFactory>();
        factory.CreateWorld(42, "connection").Returns(session);
        var gameSessions = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
        claims.TryClaimAsync(42, "claim").Returns(Task.FromResult(true));
        await gameSessions.TryAddWorldSession(42, "connection");
        claims.RenewAsync(42, "claim").Returns(Task.FromException<bool>(new InvalidOperationException("Redis unavailable.")));

        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(CancellationToken.None);

        terminator.Received(1).Abort(session);
        Assert.IsNull(await gameSessions.FindByMasterId(42));
    }

    [TestMethod]
    public async Task LeaseService_LostClaim_WhenAbortFails_RetainsReservationForNextCycle()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        var terminator = new FailOnceConnectionTerminator();
        var session = CreateSession(42, "connection", "claim");
        var factory = Substitute.For<IGameSessionFactory>();
        factory.CreateWorld(42, "connection").Returns(session);
        var gameSessions = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
        claims.TryClaimAsync(42, "claim").Returns(Task.FromResult(true));
        await gameSessions.TryAddWorldSession(42, "connection");
        claims.RenewAsync(42, "claim").Returns(Task.FromResult(false));

        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(CancellationToken.None);

        Assert.IsNull(await gameSessions.FindByMasterId(42));
        Assert.AreEqual(1, (await store.FindSessionsPendingAbort()).Count);

        await leaseService.RenewSessionsAsync(CancellationToken.None);

        Assert.AreEqual(0, (await store.FindSessionsPendingAbort()).Count);
    }

    [TestMethod]
    public async Task LeaseService_RenewalException_WhenAbortFails_RetainsReservationForNextCycle()
    {
        var store = new GameSessionStore();
        var claims = Substitute.For<IGameSessionClaimStore>();
        var terminator = new FailOnceConnectionTerminator();
        var session = CreateSession(42, "connection", "claim");
        var factory = Substitute.For<IGameSessionFactory>();
        factory.CreateWorld(42, "connection").Returns(session);
        var gameSessions = GameSessionTestDependencies.CreateService(store, store, factory, claims, terminator);
        claims.TryClaimAsync(42, "claim").Returns(Task.FromResult(true));
        await gameSessions.TryAddWorldSession(42, "connection");
        claims.RenewAsync(42, "claim").Returns(Task.FromException<bool>(new InvalidOperationException("Redis unavailable.")));

        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(CancellationToken.None);

        Assert.IsNull(await gameSessions.FindByMasterId(42));
        Assert.AreEqual(1, (await store.FindSessionsPendingAbort()).Count);

        await leaseService.RenewSessionsAsync(CancellationToken.None);

        Assert.AreEqual(0, (await store.FindSessionsPendingAbort()).Count);
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task LeaseLoss_FailedReleaseIsReconciledBeforeLaterLogin()
    {
        var store = new GameSessionStore();
        var claims = new InMemoryGameSessionClaimStore();
        var initialSession = CreateSession(42, "connection-1", "claim-1");
        var laterSession = CreateSession(42, "connection-2", "claim-2");
        var factory = Substitute.For<IGameSessionFactory>();
        factory.CreateWorld(42, "connection-1").Returns(initialSession);
        factory.CreateWorld(42, "connection-2").Returns(laterSession);
        var service = GameSessionTestDependencies.CreateService(
            store, store, factory, claims, Substitute.For<IGameSessionConnectionTerminator>());
        var initialRegistration = await service.TryAddWorldSession(42, "connection-1");
        Assert.IsTrue(await service.CommitWorldSession(initialRegistration.Session!));

        claims.Replace(42, "replacement-claim");
        Assert.IsTrue(await service.RemoveSession(initialRegistration.Session!));
        Assert.IsNull(await service.FindByMasterId(42));

        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var leaseService = GameSessionTestDependencies.CreateLeaseService(store, store, claims, terminator);
        await leaseService.RenewSessionsAsync(CancellationToken.None);

        Assert.IsNull(await service.FindByMasterId(42));

        claims.Remove(42);
        var laterRegistration = await service.TryAddWorldSession(42, "connection-2");

        Assert.IsTrue(laterRegistration.Created);
        Assert.AreSame(laterSession, laterRegistration.Session);
    }

    private static GameSessionService CreateService(
        IGameSessionClaimStore claims,
        uint masterId,
        string connectionId,
        string claimId)
    {
        var factory = Substitute.For<IGameSessionFactory>();
        var session = CreateSession(masterId, connectionId, claimId);
        factory.CreateWorld(masterId, connectionId).Returns(session);
        var store = new GameSessionStore();
        return GameSessionTestDependencies.CreateService(
            store, store, factory, claims, Substitute.For<IGameSessionConnectionTerminator>());
    }

    private static GameSessionAbortCoordinator CreateAbortCoordinator(
        GameSessionStore store,
        IGameSessionConnectionTerminator terminator) =>
        new(
            store,
            store,
            terminator,
            NullLogger<GameSessionAbortCoordinator>.Instance);

    private sealed class CompletionFailingAbortStore : IGameSessionAbortState
    {
        private readonly GameSessionStore _store;
        private bool _failCompletion = true;

        public CompletionFailingAbortStore(GameSessionStore store) => _store = store;

        public int ReleaseCalls { get; private set; }

        public ValueTask<bool> TryMoveToPendingAbort(IGameSession expectedSession) =>
            _store.TryMoveToPendingAbort(expectedSession);

        public ValueTask<(bool Began, Guid ProcessingToken)> TryBeginPendingSessionAbort(IGameSession expectedSession) =>
            _store.TryBeginPendingSessionAbort(expectedSession);

        public ValueTask<bool> TryCompletePendingSessionAbort(IGameSession expectedSession, Guid processingToken)
        {
            if (_failCompletion)
            {
                _failCompletion = false;
                return new(false);
            }

            return _store.TryCompletePendingSessionAbort(expectedSession, processingToken);
        }

        public ValueTask<bool> TryReleasePendingSessionAbort(IGameSession expectedSession, Guid processingToken)
        {
            ReleaseCalls++;
            return _store.TryReleasePendingSessionAbort(expectedSession, processingToken);
        }

        public ValueTask<IReadOnlyList<IGameSession>> FindSessionsPendingAbort() =>
            _store.FindSessionsPendingAbort();
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }

    private static IGameWorldSession CreateSession(uint masterId, string connectionId, string claimId)
    {
        var session = Substitute.For<IGameWorldSession>();
        session.MasterId.Returns(masterId);
        session.ConnectionId.Returns(connectionId);
        session.SessionClaimId.Returns(claimId);
        return session;
    }

    private static IGameSession CreateLobbySession(uint masterId, string connectionId)
    {
        var session = Substitute.For<IGameSession>();
        session.MasterId.Returns(masterId);
        session.ConnectionId.Returns(connectionId);
        return session;
    }

    private class InMemoryGameSessionClaimStore : IGameSessionClaimStore
    {
        private readonly object _sync = new();
        private readonly Dictionary<uint, string> _claims = new();

        public int Count
        {
            get
            {
                lock (_sync) return _claims.Count;
            }
        }

        public virtual Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                return Task.FromResult(_claims.TryAdd(masterId, claimId));
            }
        }

        public virtual Task<bool> ReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (!_claims.TryGetValue(masterId, out var current) || current != claimId)
                {
                    return Task.FromResult(false);
                }

                _claims.Remove(masterId);
                return Task.FromResult(true);
            }
        }

        public virtual Task<bool> RenewAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Get(masterId) == claimId);

        public virtual async Task<bool> ExecuteIfOwnerAsync(
            uint masterId,
            string claimId,
            Func<CancellationToken, Task<bool>> action,
            CancellationToken cancellationToken = default)
        {
            if (Get(masterId) != claimId)
            {
                return false;
            }

            return await action(cancellationToken);
        }

        public void Replace(uint masterId, string claimId)
        {
            lock (_sync) _claims[masterId] = claimId;
        }

        public void Remove(uint masterId)
        {
            lock (_sync) _claims.Remove(masterId);
        }

        public string? Get(uint masterId)
        {
            lock (_sync) return _claims.GetValueOrDefault(masterId);
        }
    }

    private sealed class BarrierGameSessionClaimStore : InMemoryGameSessionClaimStore
    {
        private readonly TaskCompletionSource<bool> _bothAttempts = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _releaseAttempts = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _tryClaimAttempts;

        public int TryClaimAttempts => Volatile.Read(ref _tryClaimAttempts);

        public Task WaitForBothClaimAttemptsAsync() => _bothAttempts.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public void ReleaseClaimAttempts() => _releaseAttempts.TrySetResult(true);

        public override async Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _tryClaimAttempts) == 2)
            {
                _bothAttempts.TrySetResult(true);
            }

            await _releaseAttempts.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return await base.TryClaimAsync(masterId, claimId);
        }
    }

    private sealed class PersistThenThrowGameSessionClaimStore : IGameSessionClaimStore
    {
        private bool _throwOnNextClaim = true;

        public string? CurrentClaim { get; private set; }
        public int ReleaseCalls { get; private set; }

        public Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            if (_throwOnNextClaim)
            {
                _throwOnNextClaim = false;
                CurrentClaim = claimId;
                return Task.FromException<bool>(new InvalidOperationException("Claim lock release failed."));
            }

            CurrentClaim = claimId;
            return Task.FromResult(true);
        }

        public Task<bool> ReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            ReleaseCalls++;
            if (CurrentClaim != claimId)
            {
                return Task.FromResult(false);
            }

            CurrentClaim = null;
            return Task.FromResult(true);
        }

        public Task<bool> RenewAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentClaim == claimId);

        public async Task<bool> ExecuteIfOwnerAsync(
            uint masterId,
            string claimId,
            Func<CancellationToken, Task<bool>> action,
            CancellationToken cancellationToken = default)
        {
            if (CurrentClaim != claimId)
            {
                return false;
            }

            return await action(cancellationToken);
        }
    }

    private sealed class CancelAfterPersistGameSessionClaimStore : IGameSessionClaimStore
    {
        private readonly CancellationTokenSource _cancellationSource;
        private bool _cancelNextClaim = true;

        public CancelAfterPersistGameSessionClaimStore(CancellationTokenSource cancellationSource) =>
            _cancellationSource = cancellationSource;

        public string? CurrentClaim { get; private set; }
        public int ReleaseCalls { get; private set; }

        public Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            if (!_cancelNextClaim)
            {
                CurrentClaim = claimId;
                return Task.FromResult(true);
            }

            _cancelNextClaim = false;
            CurrentClaim = claimId;
            _cancellationSource.Cancel();
            return Task.FromException<bool>(new OperationCanceledException("Claim operation canceled after persistence.", cancellationToken));
        }

        public Task<bool> ReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            ReleaseCalls++;
            if (cancellationToken.IsCancellationRequested || CurrentClaim != claimId)
            {
                return Task.FromResult(false);
            }

            CurrentClaim = null;
            return Task.FromResult(true);
        }

        public Task<bool> RenewAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentClaim == claimId);

        public async Task<bool> ExecuteIfOwnerAsync(
            uint masterId,
            string claimId,
            Func<CancellationToken, Task<bool>> action,
            CancellationToken cancellationToken = default)
        {
            if (CurrentClaim != claimId)
            {
                return false;
            }

            return await action(cancellationToken);
        }
    }

    private sealed class PersistThenThrowReleaseGameSessionClaimStore : IGameSessionClaimStore
    {
        public string? CurrentClaim { get; private set; }
        public int ReleaseCalls { get; private set; }

        public Task<bool> TryClaimAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            CurrentClaim = claimId;
            return Task.FromException<bool>(new InvalidOperationException("Claim acquisition failed after persistence."));
        }

        public Task<bool> ReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
        {
            ReleaseCalls++;
            if (ReleaseCalls == 1)
            {
                return Task.FromException<bool>(new InvalidOperationException("Claim release unavailable."));
            }

            if (CurrentClaim != claimId)
            {
                return Task.FromResult(false);
            }

            CurrentClaim = null;
            return Task.FromResult(true);
        }

        public Task<bool> RenewAsync(uint masterId, string claimId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentClaim == claimId);

        public async Task<bool> ExecuteIfOwnerAsync(
            uint masterId,
            string claimId,
            Func<CancellationToken, Task<bool>> action,
            CancellationToken cancellationToken = default)
        {
            if (CurrentClaim != claimId)
            {
                return false;
            }

            return await action(cancellationToken);
        }
    }

    private sealed class FailOnceConnectionTerminator : IGameSessionConnectionTerminator
    {
        public int Attempts { get; private set; }
        public IGameSession? LastAbortedSession { get; private set; }

        public void Abort(IGameSession session)
        {
            Attempts++;
            if (Attempts == 1)
            {
                throw new InvalidOperationException("Connection temporarily unavailable.");
            }

            LastAbortedSession = session;
        }
    }

    private sealed class CancelOnceConnectionTerminator : IGameSessionConnectionTerminator
    {
        private readonly CancellationTokenSource _cancellationSource;

        public CancelOnceConnectionTerminator(CancellationTokenSource cancellationSource) =>
            _cancellationSource = cancellationSource;

        public int Attempts { get; private set; }
        public IGameSession? LastAbortedSession { get; private set; }

        public void Abort(IGameSession session)
        {
            Attempts++;
            if (Attempts == 1)
            {
                _cancellationSource.Cancel();
                throw new OperationCanceledException(_cancellationSource.Token);
            }

            LastAbortedSession = session;
        }
    }

    private sealed class AlwaysFailConnectionTerminator : IGameSessionConnectionTerminator
    {
        public int Attempts { get; private set; }

        public void Abort(IGameSession session)
        {
            Attempts++;
            throw new InvalidOperationException("Connection is still unavailable.");
        }
    }
}
