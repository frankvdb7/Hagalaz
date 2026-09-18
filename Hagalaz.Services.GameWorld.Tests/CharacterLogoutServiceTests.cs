using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
