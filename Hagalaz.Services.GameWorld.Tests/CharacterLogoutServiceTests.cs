using System;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
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
        var state = new CharacterPersistenceState();
        state.TrackPendingLogout(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 7L);
        state.Acknowledge(42u, correlationId, 7L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, characterService, mediator);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => coordinator.DetachAsync(character));

        Assert.IsTrue(state.IsPendingLogout(character));
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task CompleteAsync_IsIdempotentAndPublishesOnlyOnce()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        character.IsDestroyed.Returns(false);
        var state = new CharacterPersistenceState();
        state.TrackPendingLogout(character);
        state.MarkPendingLogoutRemoved(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 7L);
        state.Acknowledge(42u, correlationId, 7L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, characterService, mediator);

        Assert.IsTrue(await coordinator.CompleteAsync(42u));
        Assert.IsFalse(await coordinator.CompleteAsync(42u));

        character.Received(1).Destroy();
        mediator.Received(1).Publish(Arg.Is<WorldSignOutCommand>(message => message != null && message.MasterId == 42u));
    }

    [TestMethod]
    public async Task AcknowledgeAndCompleteAsync_ConflictRetainsPendingLogout()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var state = new CharacterPersistenceState();
        state.TrackPendingLogout(character);
        state.MarkPendingLogoutRemoved(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 7L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, characterService, mediator);

        var completed = await coordinator.AcknowledgeAndCompleteAsync(
            42u,
            correlationId,
            7L,
            outcome: CharacterPersistenceOutcome.Conflict);

        Assert.IsFalse(completed);
        Assert.IsFalse(state.IsPersistenceAcknowledged(42u));
        Assert.IsTrue(state.IsPendingLogout(character));
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task AcknowledgeAndCompleteAsync_DuplicateCompletesPendingLogout()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        character.IsDestroyed.Returns(false);
        var state = new CharacterPersistenceState();
        state.TrackPendingLogout(character);
        state.MarkPendingLogoutRemoved(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 7L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, characterService, mediator);

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
        var state = new CharacterPersistenceState();
        state.TrackPendingLogout(character);
        state.MarkPendingLogoutRemoved(character);
        state.MarkPending(42u, Guid.NewGuid(), "replacement", 101L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, characterService, mediator);

        var completed = await coordinator.AcknowledgeAndCompleteAsync(
            42u,
            Guid.NewGuid(),
            101L,
            outcome: CharacterPersistenceOutcome.Committed);

        Assert.IsFalse(completed);
        Assert.IsFalse(state.IsPersistenceAcknowledged(42u));
        Assert.IsTrue(state.IsPendingLogout(character));
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task AcknowledgeAndCompleteAsync_MissingOrUnknownOutcomeRetainsPendingLogout()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var state = new CharacterPersistenceState();
        state.TrackPendingLogout(character);
        state.MarkPendingLogoutRemoved(character);
        var correlationId = Guid.NewGuid();
        state.MarkPending(42u, correlationId, "fingerprint", 101L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, characterService, mediator);
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
        Assert.IsTrue(state.IsPendingLogout(character));
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }
}
