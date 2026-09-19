using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterLogoutServiceTests
{
    [TestMethod]
    public void TryBeginLogout_AllowsSameCharacterToJoinBeforeReceiptExists()
    {
        var state = new CharacterLogoutState();
        var character = CreateCharacter(42);

        Assert.IsTrue(state.TryBeginLogout(character, out var receipt));
        Assert.IsNull(receipt);

        Assert.IsTrue(state.TryBeginLogout(character, out receipt));
        Assert.IsNull(receipt);
        Assert.IsFalse(state.TryGetPersistenceReceipt(character, out receipt));
        Assert.IsNull(receipt);

        var expectedReceipt = new CharacterPersistenceReceipt(42, Guid.NewGuid(), 7);
        Assert.IsTrue(state.SetPersistenceReceipt(character, expectedReceipt));
        Assert.IsTrue(state.TryBeginLogout(character, out receipt));
        Assert.AreSame(expectedReceipt, receipt);
        Assert.IsTrue(state.TryGetPersistenceReceipt(character, out receipt));
        Assert.AreSame(expectedReceipt, receipt);
    }

    [TestMethod]
    public void TryBeginLogout_RejectsDifferentCharacterInstanceWithSameMasterId()
    {
        var state = new CharacterLogoutState();
        var first = CreateCharacter(42);
        var second = CreateCharacter(42);
        Assert.IsTrue(state.TryBeginLogout(first, out _));

        Assert.IsFalse(state.TryBeginLogout(second, out var receipt));
        Assert.IsNull(receipt);
    }

    [TestMethod]
    public void CompleteLogout_ReleasesPersistenceStateForTheCompletedLifecycle()
    {
        var character = CreateCharacter(42);
        var persistenceState = new CharacterPersistenceState();
        persistenceState.InitializeRevision(42, 500, 7);
        var logoutState = new CharacterLogoutState();
        Assert.IsTrue(logoutState.TryBeginLogout(character, out _));
        var mediator = Substitute.For<IGameMediator>();
        var service = new CharacterLogoutService(
            logoutState,
            Substitute.For<ICharacterService>(),
            new InlineTaskScheduler(),
            mediator,
            persistenceState);

        service.CompleteLogout(character);

        Assert.AreEqual(1L, persistenceState.NextRevision(42));
        mediator.Received(1).Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public void CompleteLogout_DoesNotReleaseNewerPersistenceLifecycle()
    {
        var oldCharacter = CreateCharacter(42);
        var newCharacter = CreateCharacter(42);
        newCharacter.Session.SessionGeneration.Returns(8L);
        var persistenceState = new CharacterPersistenceState();
        persistenceState.InitializeRevision(42, 500, 7);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(oldCharacter, out _));
        Assert.IsTrue(state.SetSnapshot(oldCharacter, new CharacterModel { SnapshotRevision = 501 }));
        persistenceState.InitializeRevision(42, 900, 8);
        var mediator = Substitute.For<IGameMediator>();
        var service = new CharacterLogoutService(
            state,
            Substitute.For<ICharacterService>(),
            new InlineTaskScheduler(),
            mediator,
            persistenceState);

        service.CompleteLogout(oldCharacter);

        Assert.AreEqual(901L, persistenceState.NextRevision(42));
        Assert.IsTrue(state.TryBeginLogout(newCharacter, out _));
        mediator.Received(1).Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public void CompleteLogout_WhenPersistenceStateIsAlreadyAbsent_CompletesLogout()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _));
        var mediator = Substitute.For<IGameMediator>();
        var service = new CharacterLogoutService(
            state,
            Substitute.For<ICharacterService>(),
            new InlineTaskScheduler(),
            mediator,
            new CharacterPersistenceState());

        service.CompleteLogout(character);

        Assert.IsFalse(state.IsPending(character));
        mediator.Received(1).Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public void CompleteLogout_WhenPersistenceReleaseIsRefused_RetainsRecoverableLogout()
    {
        var character = CreateCharacter(42);
        var receipt = new CharacterPersistenceReceipt(42, Guid.NewGuid(), 7);
        var persistenceState = new CharacterPersistenceState();
        persistenceState.InitializeRevision(42, 500, 7);
        persistenceState.MarkPending(42, "snapshot", receipt);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _));
        Assert.IsTrue(state.SetSnapshot(character, new CharacterModel { SnapshotRevision = 501 }));
        var mediator = Substitute.For<IGameMediator>();
        var service = new CharacterLogoutService(
            state,
            Substitute.For<ICharacterService>(),
            new InlineTaskScheduler(),
            mediator,
            persistenceState);

        service.CompleteLogout(character);

        Assert.IsTrue(state.IsPending(character));
        Assert.AreEqual(1, state.FindRecoverablePendingLogouts().Count);
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task DetachAsync_CapturesSnapshotRemovesExactOwnerAndDestroysOnce()
    {
        var character = CreateCharacter(42);
        var order = new List<string>();
        var state = new CharacterLogoutState();
        state.TryBeginLogout(character, out _);
        var characterService = Substitute.For<ICharacterService>();
        characterService.Remove(character).Returns(true);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        var snapshot = new CharacterModel();
        dehydrationService.Dehydrate(character).Returns(_ =>
        {
            order.Add("snapshot");
            return snapshot;
        });
        characterService.Remove(character).Returns(_ =>
        {
            order.Add("remove");
            return true;
        });
        character.When(value => value.Destroy()).Do(_ => order.Add("destroy"));
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        var service = new CharacterLogoutService(
            state,
            characterService,
            new InlineTaskScheduler(),
            Substitute.For<IGameMediator>(),
            new CharacterPersistenceState());

        var result = await service.DetachAsync(character);

        Assert.AreNotSame(snapshot, result);
        Assert.AreEqual(1L, result.SnapshotRevision);
        Assert.AreEqual(0L, snapshot.SnapshotRevision);
        characterService.Received(1).Remove(character);
        character.Received(1).Destroy();
        CollectionAssert.AreEqual(new[] { "snapshot", "remove", "destroy" }, order);
    }

    [TestMethod]
    public async Task DetachAsync_WhenWaitIsCanceledBeforeWorkerRuns_MakesTerminalSnapshotRecoverable()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _));
        var characterService = Substitute.For<ICharacterService>();
        characterService.Remove(character).Returns(true);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);

        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var persistenceState = new CharacterPersistenceState();
        persistenceState.InitializeRevision(42, 0, 7);
        var persistence = Substitute.For<ICharacterPersistenceService>();
        var receipt = new CharacterPersistenceReceipt(42, Guid.NewGuid(), 1);
        persistence.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CharacterPersistenceReceipt?>(receipt));
        persistence.WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CharacterPersistenceOutcome.Committed));
        var sessionService = Substitute.For<IGameSessionService>();
        sessionService.RemoveSession(character.Session, CancellationToken.None).Returns(Task.FromResult(true));
        var mediator = Substitute.For<IGameMediator>();
        var service = new CharacterLogoutService(
            state,
            characterService,
            scheduler,
            mediator,
            persistenceState,
            persistence,
            sessionService);
        using var cancellation = new CancellationTokenSource();

        var detachTask = service.DetachAsync(character, cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => detachTask);
        Assert.AreEqual(0, state.FindRecoverablePendingLogouts().Count);

        scheduler.Tick();

        Assert.AreEqual(1, state.FindRecoverablePendingLogouts().Count);
        Assert.IsTrue(state.TryGetSnapshot(character, out var snapshot));
        await service.RecoverPendingLogoutsAsync();

        await persistence.Received(1).PersistAsync(42, snapshot, true, Arg.Any<CancellationToken>());
        await persistence.Received(1).WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>());
        await sessionService.Received(1).RemoveSession(character.Session, CancellationToken.None);
        Assert.IsFalse(state.IsPending(character));
        mediator.Received(1).Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task DetachAsync_WhenDehydrationFailsBeforeSnapshot_DoesNotMakeLogoutRecoverable()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _));
        var characterService = Substitute.For<ICharacterService>();
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        var failure = new InvalidOperationException("dehydration failed");
        dehydrationService.Dehydrate(character).Returns(_ => throw failure);
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);

        var service = new CharacterLogoutService(
            state,
            characterService,
            new InlineTaskScheduler(),
            Substitute.For<IGameMediator>(),
            new CharacterPersistenceState());

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.DetachAsync(character));

        Assert.AreSame(failure, exception);
        Assert.AreEqual(0, state.FindRecoverablePendingLogouts().Count);
        Assert.IsFalse(state.TryGetSnapshot(character, out _));
        characterService.DidNotReceive().Remove(character);
    }

    [TestMethod]
    public async Task DetachAsync_WhenCleanupFailsAfterSnapshot_MakesLogoutRecoverable()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _));
        var characterService = Substitute.For<ICharacterService>();
        characterService.Remove(character).Returns(true);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        var failure = new InvalidOperationException("destroy failed");
        character.When(value => value.Destroy()).Do(_ => throw failure);

        var service = new CharacterLogoutService(
            state,
            characterService,
            new InlineTaskScheduler(),
            Substitute.For<IGameMediator>(),
            new CharacterPersistenceState());

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.DetachAsync(character));

        Assert.AreSame(failure, exception);
        Assert.AreEqual(1, state.FindRecoverablePendingLogouts().Count);
        Assert.IsTrue(state.TryGetSnapshot(character, out _));
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task RecoverPendingLogouts_ClaimsRecoveryExactlyOnce()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _));
        Assert.IsTrue(state.SetSnapshot(character, new CharacterModel { SnapshotRevision = 1 }));
        Assert.IsTrue(state.MarkRecoveryEligible(character));

        var persistenceState = new CharacterPersistenceState();
        persistenceState.InitializeRevision(42, 0, 7);
        var persistence = Substitute.For<ICharacterPersistenceService>();
        var receipt = new CharacterPersistenceReceipt(42, Guid.NewGuid(), 1);
        var persistenceStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var acknowledge = new TaskCompletionSource<CharacterPersistenceOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        persistence.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                persistenceStarted.TrySetResult(true);
                return Task.FromResult<CharacterPersistenceReceipt?>(receipt);
            });
        persistence.WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>())
            .Returns(acknowledge.Task);
        var sessionService = Substitute.For<IGameSessionService>();
        sessionService.RemoveSession(character.Session, CancellationToken.None).Returns(Task.FromResult(true));
        var mediator = Substitute.For<IGameMediator>();
        var service = new CharacterLogoutService(
            state,
            Substitute.For<ICharacterService>(),
            new InlineTaskScheduler(),
            mediator,
            persistenceState,
            persistence,
            sessionService);

        var firstRecovery = service.RecoverPendingLogoutsAsync();
        await persistenceStarted.Task;
        var secondRecovery = service.RecoverPendingLogoutsAsync();

        await secondRecovery;
        await persistence.Received(1).PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>());
        await sessionService.DidNotReceive().RemoveSession(Arg.Any<IGameSession>(), Arg.Any<CancellationToken>());

        acknowledge.TrySetResult(CharacterPersistenceOutcome.Committed);
        await firstRecovery;

        await sessionService.Received(1).RemoveSession(character.Session, CancellationToken.None);
        mediator.Received(1).Publish(Arg.Any<WorldSignOutCommand>());
        Assert.IsFalse(state.IsPending(character));
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task DetachAsync_WhenCanceledAttemptFailsBeforeSnapshot_DoesNotPoisonSuccessfulRetry()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _));
        var characterService = Substitute.For<ICharacterService>();
        characterService.Remove(character).Returns(false);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var persistence = Substitute.For<ICharacterPersistenceService>();
        var sessionService = Substitute.For<IGameSessionService>();
        var service = new CharacterLogoutService(
            state,
            characterService,
            scheduler,
            Substitute.For<IGameMediator>(),
            new CharacterPersistenceState(),
            persistence,
            sessionService);
        using var cancellation = new CancellationTokenSource();

        var canceledAttempt = service.DetachAsync(character, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => canceledAttempt);
        scheduler.Tick();

        characterService.Remove(character).Returns(true);
        var retry = service.DetachAsync(character);
        scheduler.Tick();
        var snapshot = await retry;

        Assert.IsNotNull(snapshot);
        Assert.IsFalse(state.FindRecoverablePendingLogouts().Count > 0);
        await service.RecoverPendingLogoutsAsync();
        await persistence.DidNotReceive().PersistAsync(
            Arg.Any<uint>(),
            Arg.Any<CharacterModel>(),
            true,
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task DetachAsync_WhenCancellationWinsAfterSnapshotPublication_MakesSnapshotRecoverable()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _));
        var characterService = Substitute.For<ICharacterService>();
        characterService.Remove(character).Returns(true);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        var snapshotPublished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseDestroy = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        character.When(value => value.Destroy()).Do(_ =>
        {
            snapshotPublished.TrySetResult(true);
            releaseDestroy.Task.GetAwaiter().GetResult();
        });
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CharacterLogoutService(
            state,
            characterService,
            scheduler,
            Substitute.For<IGameMediator>(),
            new CharacterPersistenceState());
        using var cancellation = new CancellationTokenSource();

        var detach = service.DetachAsync(character, cancellation.Token);
        var worker = Task.Run(scheduler.Tick);
        await snapshotPublished.Task;
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => detach);
        Assert.AreEqual(1, state.FindRecoverablePendingLogouts().Count);

        releaseDestroy.TrySetResult(true);
        await worker;
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task RecoverPendingLogouts_WhenDestroyIsBlocked_CompletesRecoveryBeforeWorkerFinishes()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _));
        var characterService = Substitute.For<ICharacterService>();
        characterService.Remove(character).Returns(true);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        var snapshotPublished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseDestroy = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        character.When(value => value.Destroy()).Do(_ =>
        {
            snapshotPublished.TrySetResult(true);
            releaseDestroy.Task.GetAwaiter().GetResult();
        });
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var persistenceState = new CharacterPersistenceState();
        persistenceState.InitializeRevision(42, 0, 7);
        var persistence = Substitute.For<ICharacterPersistenceService>();
        var receipt = new CharacterPersistenceReceipt(42, Guid.NewGuid(), 1);
        var persistenceStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var acknowledge = new TaskCompletionSource<CharacterPersistenceOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        persistence.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                persistenceStarted.TrySetResult(true);
                return Task.FromResult<CharacterPersistenceReceipt?>(receipt);
            });
        persistence.WaitForAcknowledgementAsync(receipt, Arg.Any<CancellationToken>())
            .Returns(acknowledge.Task);
        var sessionService = Substitute.For<IGameSessionService>();
        sessionService.RemoveSession(character.Session, CancellationToken.None).Returns(Task.FromResult(true));
        var mediator = Substitute.For<IGameMediator>();
        var service = new CharacterLogoutService(
            state,
            characterService,
            scheduler,
            mediator,
            persistenceState,
            persistence,
            sessionService);
        using var cancellation = new CancellationTokenSource();

        var detach = service.DetachAsync(character, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => detach);
        var worker = Task.Run(scheduler.Tick);
        await snapshotPublished.Task;

        var recovery = service.RecoverPendingLogoutsAsync();
        await persistenceStarted.Task;
        acknowledge.TrySetResult(CharacterPersistenceOutcome.Committed);
        await recovery;

        await persistence.Received(1).PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>());
        await sessionService.Received(1).RemoveSession(character.Session, CancellationToken.None);
        mediator.Received(1).Publish(Arg.Any<WorldSignOutCommand>());
        Assert.IsFalse(state.IsPending(character));

        releaseDestroy.TrySetResult(true);
        await worker;
    }

    [TestMethod]
    public async Task RecoverPendingLogouts_UsesCallerCancellationTokenForPersistence()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _));
        Assert.IsTrue(state.SetSnapshot(character, new CharacterModel { SnapshotRevision = 1 }));
        Assert.IsTrue(state.MarkRecoveryEligible(character));

        var persistenceState = new CharacterPersistenceState();
        persistenceState.InitializeRevision(42, 0, 7);
        var persistence = Substitute.For<ICharacterPersistenceService>();
        var tokenObserved = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var persistenceTask = new TaskCompletionSource<CharacterPersistenceReceipt?>(TaskCreationOptions.RunContinuationsAsynchronously);
        persistence.PersistAsync(42, Arg.Any<CharacterModel>(), true, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                tokenObserved.TrySetResult(token);
                token.Register(() => persistenceTask.TrySetCanceled(token));
                return persistenceTask.Task;
            });
        var service = new CharacterLogoutService(
            state,
            Substitute.For<ICharacterService>(),
            new InlineTaskScheduler(),
            Substitute.For<IGameMediator>(),
            persistenceState,
            persistence,
            Substitute.For<IGameSessionService>());
        using var cancellation = new CancellationTokenSource();

        var recovery = service.RecoverPendingLogoutsAsync(cancellation.Token);
        var suppliedToken = await tokenObserved.Task;

        Assert.AreNotEqual(default, suppliedToken);
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => recovery);
        Assert.IsTrue(state.IsPending(character));
    }

    [TestMethod]
    public async Task DetachAsync_RunsAdmittedGameplayBeforeCapturingSnapshot()
    {
        var character = CreateCharacter(42);
        var order = new List<string>();
        var state = new CharacterLogoutState();
        var characterService = Substitute.For<ICharacterService>();
        characterService.Remove(character).Returns(_ =>
        {
            order.Add("remove");
            return true;
        });
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var creatureTaskService = new CreatureTaskService(scheduler);
        var gameplayApplied = false;
        creatureTaskService.Queue(new RsTask(() =>
        {
            gameplayApplied = true;
            order.Add("gameplay");
        }, 1), CancellationToken.None);
        Assert.IsTrue(state.TryBeginLogout(character, out _));

        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        var snapshot = new CharacterModel();
        dehydrationService.Dehydrate(character).Returns(_ =>
        {
            Assert.IsTrue(gameplayApplied);
            order.Add("snapshot");
            return snapshot;
        });
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        character.When(value => value.Destroy()).Do(_ => order.Add("destroy"));
        var logout = new CharacterLogoutService(
            state,
            characterService,
            scheduler,
            Substitute.For<IGameMediator>(),
            new CharacterPersistenceState());

        var detachTask = logout.DetachAsync(character);
        scheduler.Tick();

        var result = await detachTask;
        Assert.AreNotSame(snapshot, result);
        Assert.AreEqual(1L, result.SnapshotRevision);
        Assert.AreEqual(0L, snapshot.SnapshotRevision);
        CollectionAssert.AreEqual(new[] { "gameplay", "snapshot", "remove", "destroy" }, order);
    }

    [TestMethod]
    public async Task DetachAsync_WhenOwnershipRemovalFails_RetainsClaimAndDoesNotDestroy()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        state.TryBeginLogout(character, out _);
        var characterService = Substitute.For<ICharacterService>();
        characterService.Remove(character).Returns(false);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        var service = new CharacterLogoutService(
            state,
            characterService,
            new InlineTaskScheduler(),
            Substitute.For<IGameMediator>(),
            new CharacterPersistenceState());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.DetachAsync(character));

        Assert.IsTrue(state.IsPending(character));
        character.DidNotReceive().Destroy();
    }

    private static ICharacter CreateCharacter(uint masterId)
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(masterId);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        session.SessionGeneration.Returns(7L);
        character.Session.Returns(session);
        return character;
    }

    private sealed class InlineTaskScheduler : IRsTaskService
    {
        public void Schedule(ITaskItem action) => action.Tick();
        public void Tick() { }
    }
}
