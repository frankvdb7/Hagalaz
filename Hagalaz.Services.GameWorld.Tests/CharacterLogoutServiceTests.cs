using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Services;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterLogoutServiceTests
{
    [TestMethod]
    public void TryBeginLogout_ClaimsNewCharacterAndReturnsSameReceiptOnDuplicate()
    {
        var state = new CharacterLogoutState();
        var character = CreateCharacter(42);

        Assert.IsTrue(state.TryBeginLogout(character, out var created, out var receipt));
        Assert.IsTrue(created);
        Assert.IsNull(receipt);

        var expectedReceipt = new CharacterPersistenceReceipt(42, Guid.NewGuid(), 7);
        Assert.IsTrue(state.SetPersistenceReceipt(character, expectedReceipt));
        Assert.IsTrue(state.TryBeginLogout(character, out created, out receipt));
        Assert.IsFalse(created);
        Assert.AreSame(expectedReceipt, receipt);
    }

    [TestMethod]
    public void TryBeginLogout_RejectsDifferentCharacterInstanceWithSameMasterId()
    {
        var state = new CharacterLogoutState();
        var first = CreateCharacter(42);
        var second = CreateCharacter(42);

        Assert.IsTrue(state.TryBeginLogout(first, out _, out _));

        Assert.IsFalse(state.TryBeginLogout(second, out var created, out var receipt));
        Assert.IsFalse(created);
        Assert.IsNull(receipt);
        Assert.IsTrue(state.IsPending(first));
        Assert.IsFalse(state.IsPending(second));
    }

    [TestMethod]
    public async Task DetachAsync_WhenCharacterRemovalFails_RetainsClaimAndDoesNotDestroy()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        state.TryBeginLogout(character, out _, out _);
        var characterService = Substitute.For<ICharacterService>();
        characterService.RemoveAsync(character).Returns(false);
        var mediator = Substitute.For<IGameMediator>();
        var service = new CharacterLogoutService(state, characterService, mediator);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.DetachAsync(character));

        Assert.IsTrue(state.IsPending(character));
        character.DidNotReceive().Destroy();
        mediator.DidNotReceive().Publish(Arg.Any<WorldSignOutCommand>());
    }

    [TestMethod]
    public async Task DetachAsync_WhenRemovalAndDestroySucceed_PublishesAndReleasesClaim()
    {
        var character = CreateCharacter(42);
        character.IsDestroyed.Returns(false);
        var state = new CharacterLogoutState();
        state.TryBeginLogout(character, out _, out _);
        var characterService = Substitute.For<ICharacterService>();
        characterService.RemoveAsync(character).Returns(true);
        var mediator = Substitute.For<IGameMediator>();
        var service = new CharacterLogoutService(state, characterService, mediator);

        await service.DetachAsync(character);

        await characterService.Received(1).RemoveAsync(character);
        character.Received(1).Destroy();
        mediator.Received(1).Publish(Arg.Is<WorldSignOutCommand>(message => message.MasterId == 42));
        Assert.IsFalse(state.IsPending(character));
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
}
