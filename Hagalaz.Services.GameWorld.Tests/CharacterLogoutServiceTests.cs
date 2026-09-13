using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.DependencyInjection;
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
    }

    [TestMethod]
    public void TryAdmit_RejectsGameplayAfterLogoutClaim()
    {
        var state = new CharacterLogoutState();
        var character = CreateCharacter(42);
        state.TryBeginLogout(character, out _, out _);

        Assert.IsFalse(state.TryAdmit(character, () => true));
    }

    [TestMethod]
    public async Task DetachAsync_CapturesSnapshotRemovesExactOwnerAndDestroysOnce()
    {
        var character = CreateCharacter(42);
        var order = new List<string>();
        var state = new CharacterLogoutState();
        state.TryBeginLogout(character, out _, out _);
        var store = Substitute.For<ICharacterStore>();
        store.IsCurrent(character).Returns(true);
        store.Remove(character).Returns(true);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        var snapshot = new CharacterModel();
        dehydrationService.Dehydrate(character).Returns(_ =>
        {
            order.Add("snapshot");
            return snapshot;
        });
        store.Remove(character).Returns(_ =>
        {
            order.Add("remove");
            return true;
        });
        character.When(value => value.Destroy()).Do(_ => order.Add("destroy"));
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        var service = new CharacterLogoutService(state, store, new InlineTaskScheduler(), Substitute.For<IGameMediator>());

        var result = await service.DetachAsync(character);

        Assert.AreSame(snapshot, result);
        store.Received(1).Remove(character);
        character.Received(1).Destroy();
        CollectionAssert.AreEqual(new[] { "snapshot", "remove", "destroy" }, order);
    }

    [TestMethod]
    public async Task DetachAsync_WhenOwnershipRemovalFails_RetainsClaimAndDoesNotDestroy()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        state.TryBeginLogout(character, out _, out _);
        var store = Substitute.For<ICharacterStore>();
        store.IsCurrent(character).Returns(true);
        store.Remove(character).Returns(false);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        var service = new CharacterLogoutService(state, store, new InlineTaskScheduler(), Substitute.For<IGameMediator>());

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
