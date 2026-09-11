using System;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterLogoutServiceTests
{
    [TestMethod]
    public async Task DetachAsync_WhenRemovalFails_RetainsPendingLogout()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        session.SessionGeneration.Returns(7L);
        character.Session.Returns(session);
        var state = new CharacterPersistenceState();
        var logoutState = new CharacterLogoutState();
        logoutState.Track(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 7L);
        state.Acknowledge(42u, correlationId, 7L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, logoutState, characterService, mediator);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => coordinator.DetachAsync(character));

        Assert.IsTrue(logoutState.IsPending(character));
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task CompleteAsync_IsIdempotentAndPublishesOnlyOnce()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        session.SessionGeneration.Returns(7L);
        character.Session.Returns(session);
        character.IsDestroyed.Returns(false);
        var state = new CharacterPersistenceState();
        var logoutState = new CharacterLogoutState();
        logoutState.Track(character);
        logoutState.TryMarkRemoved(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 7L);
        state.Acknowledge(42u, correlationId, 7L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, logoutState, characterService, mediator);

        Assert.IsTrue(await coordinator.CompleteAsync(42u));
        Assert.IsFalse(await coordinator.CompleteAsync(42u));

        character.Received(1).Destroy();
        mediator.Received(1).Publish(Arg.Is<WorldSignOutCommand>(message => message != null && message.MasterId == 42u && message.SessionGeneration == 7L && message.ConnectionId == "connection"));
    }

    [TestMethod]
    public async Task CompleteAsync_WhenCharacterDestroyFails_RetainsStateAndRetriesOnlyDestroy()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        session.SessionGeneration.Returns(7L);
        character.Session.Returns(session);
        var destroyAttempts = 0;
        character.IsDestroyed.Returns(_ => destroyAttempts > 1);
        character.When(value => value.Destroy()).Do(_ =>
        {
            if (++destroyAttempts == 1)
                throw new InvalidOperationException("destroy failed");
        });
        var state = new CharacterPersistenceState();
        var logoutState = new CharacterLogoutState();
        logoutState.Track(character);
        logoutState.TryMarkRemoved(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 7L);
        state.Acknowledge(42u, correlationId, 7L);
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, logoutState, Substitute.For<ICharacterService>(), mediator);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => coordinator.CompleteAsync(42u));
        Assert.IsTrue(logoutState.IsPending(character));
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());

        Assert.IsTrue(await coordinator.CompleteAsync(42u));
        Assert.IsFalse(logoutState.IsPending(character));
        character.Received(2).Destroy();
        mediator.Received(1).Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task CompleteAsync_WhenSignOutPublicationFails_RetainsDestroyedStepAndRetriesPublication()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        character.IsDestroyed.Returns(true);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        session.SessionGeneration.Returns(7L);
        character.Session.Returns(session);
        var mediator = Substitute.For<IGameMediator>();
        var publishFailure = true;
        mediator.When(value => value.Publish(Arg.Any<WorldSignOutCommand>())).Do(_ =>
        {
            if (publishFailure)
                throw new InvalidOperationException("publish failed");
        });
        var state = new CharacterPersistenceState();
        var logoutState = new CharacterLogoutState();
        logoutState.Track(character);
        logoutState.TryMarkRemoved(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 7L);
        state.Acknowledge(42u, correlationId, 7L);
        var coordinator = new CharacterLogoutService(state, logoutState, Substitute.For<ICharacterService>(), mediator);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => coordinator.CompleteAsync(42u));
        Assert.IsTrue(logoutState.IsPending(character));
        character.DidNotReceive().Destroy();

        publishFailure = false;
        Assert.IsTrue(await coordinator.CompleteAsync(42u));
        character.DidNotReceive().Destroy();
        mediator.Received(2).Publish(Arg.Any<WorldSignOutCommand>());
        Assert.IsFalse(logoutState.IsPending(character));
    }

    [TestMethod]
    public async Task AcknowledgeAndCompleteAsync_ConflictRetainsPendingLogout()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        character.Session.Returns(session);
        var state = new CharacterPersistenceState();
        var logoutState = new CharacterLogoutState();
        logoutState.Track(character);
        logoutState.TryMarkRemoved(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 7L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, logoutState, characterService, mediator);

        var completed = await coordinator.AcknowledgeAndCompleteAsync(
            42u,
            correlationId,
            7L,
            outcome: CharacterPersistenceOutcome.Conflict);

        Assert.IsFalse(completed);
        Assert.IsFalse(state.IsPersistenceAcknowledged(42u));
        Assert.IsTrue(logoutState.IsPending(character));
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task AcknowledgeAndCompleteAsync_DuplicateCompletesPendingLogout()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        character.Session.Returns(session);
        character.IsDestroyed.Returns(false);
        var state = new CharacterPersistenceState();
        var logoutState = new CharacterLogoutState();
        logoutState.Track(character);
        logoutState.TryMarkRemoved(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 7L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, logoutState, characterService, mediator);

        var completed = await coordinator.AcknowledgeAndCompleteAsync(
            42u,
            correlationId,
            7L,
            outcome: CharacterPersistenceOutcome.Duplicate);

        Assert.IsTrue(completed);
        character.Received(1).Destroy();
        mediator.Received(1).Publish(Arg.Is<WorldSignOutCommand>(message => message != null && message.MasterId == 42u));
    }

    [TestMethod]
    public async Task AcknowledgeAndCompleteAsync_OldCorrelationDoesNotAcknowledgeReplacementSnapshot()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        character.Session.Returns(session);
        var state = new CharacterPersistenceState();
        var logoutState = new CharacterLogoutState();
        logoutState.Track(character);
        logoutState.TryMarkRemoved(character);
        state.MarkPending(42u, Guid.NewGuid(), "replacement", 101L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, logoutState, characterService, mediator);

        var completed = await coordinator.AcknowledgeAndCompleteAsync(
            42u,
            Guid.NewGuid(),
            101L,
            outcome: CharacterPersistenceOutcome.Committed);

        Assert.IsFalse(completed);
        Assert.IsFalse(state.IsPersistenceAcknowledged(42u));
        Assert.IsTrue(logoutState.IsPending(character));
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task AcknowledgeAndCompleteAsync_MissingOrUnknownOutcomeRetainsPendingLogout()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        character.Session.Returns(session);
        var state = new CharacterPersistenceState();
        var logoutState = new CharacterLogoutState();
        logoutState.Track(character);
        logoutState.TryMarkRemoved(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 101L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, logoutState, characterService, mediator);
        var missingOutcome = new PersistCharacterAcknowledged(correlationId, 42u, 101L);
        var unknownOutcome = new PersistCharacterAcknowledged(
            correlationId,
            42u,
            101L,
            (CharacterPersistenceOutcome)99);

        var missingCompleted = await coordinator.AcknowledgeAndCompleteAsync(
            missingOutcome.MasterId,
            missingOutcome.CorrelationId,
            missingOutcome.SnapshotRevision,
            outcome: missingOutcome.Outcome);
        var unknownCompleted = await coordinator.AcknowledgeAndCompleteAsync(
            unknownOutcome.MasterId,
            unknownOutcome.CorrelationId,
            unknownOutcome.SnapshotRevision,
            outcome: unknownOutcome.Outcome);

        Assert.IsFalse(missingCompleted);
        Assert.IsFalse(unknownCompleted);
        Assert.IsFalse(state.IsPersistenceAcknowledged(42u));
        Assert.IsTrue(logoutState.IsPending(character));
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }
}
