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
    public async Task TryPreparePersistenceRetry_ReplacesDetachedSnapshotWithNextRevision()
    {
        var character = CreateCharacter(42);
        var state = new CharacterLogoutState();
        Assert.IsTrue(state.TryBeginLogout(character, out _, out _));
        var store = Substitute.For<ICharacterStore>();
        store.Remove(character).Returns(true);
        var snapshot = new CharacterModel { SnapshotRevision = 1 };
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(snapshot);
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        var persistenceState = new CharacterPersistenceState();
        var service = new CharacterLogoutService(
            state,
            store,
            new InlineTaskScheduler(),
            Substitute.For<IGameMediator>(),
            persistenceState);

        await service.DetachAsync(character);
        var firstReceipt = new CharacterPersistenceReceipt(42, Guid.NewGuid(), 1);
        Assert.IsTrue(service.SetPendingLogoutPersistence(character, firstReceipt));

        Assert.IsTrue(service.TryPreparePersistenceRetry(character, out var retrySnapshot));

        Assert.AreEqual(2L, retrySnapshot.SnapshotRevision);
        Assert.AreEqual(2L, state.TryGetSnapshot(character, out var retainedSnapshot) ? retainedSnapshot.SnapshotRevision : 0);
        Assert.IsTrue(service.TryGetPendingPersistence(character, out var retryReceipt));
        Assert.IsNull(retryReceipt);
    }

    [TestMethod]
    public async Task DetachAsync_CapturesSnapshotRemovesExactOwnerAndDestroysOnce()
    {
        var character = CreateCharacter(42);
        var order = new List<string>();
        var state = new CharacterLogoutState();
        state.TryBeginLogout(character, out _, out _);
        var store = Substitute.For<ICharacterStore>();
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
        var service = new CharacterLogoutService(
            state,
            store,
            new InlineTaskScheduler(),
            Substitute.For<IGameMediator>(),
            new CharacterPersistenceState());

        var result = await service.DetachAsync(character);

        Assert.AreNotSame(snapshot, result);
        Assert.AreEqual(1L, result.SnapshotRevision);
        Assert.AreEqual(0L, snapshot.SnapshotRevision);
        store.Received(1).Remove(character);
        character.Received(1).Destroy();
        CollectionAssert.AreEqual(new[] { "snapshot", "remove", "destroy" }, order);
    }

    [TestMethod]
    public async Task DetachAsync_RunsAdmittedGameplayBeforeCapturingSnapshot()
    {
        var character = CreateCharacter(42);
        var order = new List<string>();
        var state = new CharacterLogoutState();
        var store = Substitute.For<ICharacterStore>();
        store.Remove(character).Returns(_ =>
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
        Assert.IsTrue(state.TryBeginLogout(character, out _, out _));

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
            store,
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
        state.TryBeginLogout(character, out _, out _);
        var store = Substitute.For<ICharacterStore>();
        store.Remove(character).Returns(false);
        var dehydrationService = Substitute.For<ICharacterDehydrationService>();
        dehydrationService.Dehydrate(character).Returns(new CharacterModel());
        using var provider = new ServiceCollection()
            .AddSingleton<ICharacterDehydrationService>(dehydrationService)
            .BuildServiceProvider();
        character.ServiceProvider.Returns(provider);
        var service = new CharacterLogoutService(
            state,
            store,
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
