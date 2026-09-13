using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterExecutionServiceTests
{
    [TestMethod]
    public void Queue_WhenCharacterIsCurrent_QueuesOnGameWorker()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var task = Substitute.For<ITaskItem>();
        var store = Substitute.For<ICharacterStore>();
        var scheduler = Substitute.For<IRsTaskService>();
        store.IsCurrent(character).Returns(true);
        var service = new CharacterExecutionService(store, new CharacterLogoutState(), scheduler);

        service.Queue(character, task);

        scheduler.Received(1).Schedule(Arg.Any<ITaskItem>());
        task.DidNotReceive().Cancel();
    }

    [TestMethod]
    public void QueuedTask_WhenOwnershipIsRevoked_DoesNotRunAgain()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var store = Substitute.For<ICharacterStore>();
        store.IsCurrent(character).Returns(true);
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CharacterExecutionService(store, new CharacterLogoutState(), scheduler);
        var ticks = 0;

        service.Queue(character, new RsTickTask(() => ticks++));
        scheduler.Tick();
        Assert.AreEqual(1, ticks);

        store.IsCurrent(character).Returns(false);
        scheduler.Tick();
        scheduler.Tick();

        Assert.AreEqual(1, ticks);
    }

    [TestMethod]
    public void Queue_WhenCharacterIsReplaced_DropsTaskForReplacedInstance()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var replacement = Substitute.For<ICharacter>();
        replacement.MasterId.Returns(42u);
        var store = Substitute.For<ICharacterStore>();
        store.IsCurrent(character).Returns(true);
        store.IsCurrent(replacement).Returns(true);
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CharacterExecutionService(store, new CharacterLogoutState(), scheduler);
        var staleMutations = 0;
        var replacementMutations = 0;

        service.Queue(character, new RsTask(() => staleMutations++, 2));
        store.IsCurrent(character).Returns(false);
        service.Queue(replacement, new RsTask(() => replacementMutations++, 1));

        scheduler.Tick();

        Assert.AreEqual(0, staleMutations);
        Assert.AreEqual(1, replacementMutations);
    }

    [TestMethod]
    public void Queue_WhenCharacterIsNoLongerCurrent_CancelsWithoutQueuing()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var task = Substitute.For<ITaskItem>();
        var store = Substitute.For<ICharacterStore>();
        var scheduler = Substitute.For<IRsTaskService>();
        store.IsCurrent(character).Returns(false);
        var service = new CharacterExecutionService(store, new CharacterLogoutState(), scheduler);

        service.Queue(character, task);

        scheduler.DidNotReceive().Schedule(Arg.Any<ITaskItem>());
        task.Received(1).Cancel();
    }

    [TestMethod]
    public void Queue_WhenLogoutHasStarted_CancelsWithoutQueuing()
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var session = Substitute.For<IGameSession>();
        session.MasterId.Returns(42u);
        session.ConnectionId.Returns("connection");
        session.SessionGeneration.Returns(1L);
        character.Session.Returns(session);
        var logoutState = new CharacterLogoutState();
        Assert.IsTrue(logoutState.TryBeginLogout(character, out var created, out _));
        Assert.IsTrue(created);

        var task = Substitute.For<ITaskItem>();
        var store = Substitute.For<ICharacterStore>();
        var scheduler = Substitute.For<IRsTaskService>();
        store.IsCurrent(character).Returns(true);
        var service = new CharacterExecutionService(store, logoutState, scheduler);

        service.Queue(character, task);

        scheduler.DidNotReceive().Schedule(Arg.Any<ITaskItem>());
        task.Received(1).Cancel();
    }
}
