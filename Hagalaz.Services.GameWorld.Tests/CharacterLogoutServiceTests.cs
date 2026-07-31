using System;
using System.Threading.Tasks;
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
        state.MarkPending(42u, "fingerprint", 7L);
        state.Acknowledge(42u, 7L);
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
        state.MarkPending(42u, "fingerprint", 7L);
        state.Acknowledge(42u, 7L);
        var characterService = Substitute.For<ICharacterService>();
        var mediator = Substitute.For<IGameMediator>();
        var coordinator = new CharacterLogoutService(state, characterService, mediator);

        Assert.IsTrue(await coordinator.CompleteAsync(42u));
        Assert.IsFalse(await coordinator.CompleteAsync(42u));

        character.Received(1).Destroy();
        mediator.Received(1).Publish(Arg.Is<WorldSignOutCommand>(message => message != null && message.MasterId == 42u));
    }
}
