using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Model.Creatures.Characters;

[TestClass]
public sealed class SlayerTests
{
    private static (Slayer Slayer, IGameMediator Mediator, ICharacter Character) CreateSlayer(int? balance, bool withTask)
    {
        var character = Substitute.For<ICharacter>();
        var profile = Substitute.For<IProfile>();
        profile.TryGetValue<int>(Hagalaz.Configuration.ProfileConstants.SlayerRewardPoints, out Arg.Any<int>())
            .Returns(call =>
            {
                call[1] = balance ?? 0;
                return balance.HasValue;
            });
        character.Profile.Returns(profile);
        var mediator = Substitute.For<IGameMediator>();
        var slayer = new Slayer(
            character,
            Substitute.For<ISlayerService>(),
            Substitute.For<IRatesService>(),
            Substitute.For<ISlayerTaskGenerator>(),
            Substitute.For<ISlayerTaskCompletedDialogue>(),
            mediator);
        if (withTask)
        {
            slayer.Hydrate(new HydratedSlayerDto { Task = new HydratedSlayerDto.SlayerTaskDto { Id = 1, KillCount = 10 } });
        }

        return (slayer, mediator, character);
    }

    [TestMethod]
    [DataRow(29, false)]
    [DataRow(30, true)]
    [DataRow(31, true)]
    public void TrySkipAssignment_RequiresThirtyPoints(int balance, bool expected)
    {
        var (slayer, mediator, _) = CreateSlayer(balance, withTask: true);

        Assert.AreEqual(expected, slayer.TrySkipAssignment(30));
        Assert.AreEqual(expected ? -1 : 1, slayer.CurrentTaskId);
        mediator.Received(expected ? 1 : 0).Publish(Arg.Any<Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions.ProfileDecrementIntAction>());
    }

    [TestMethod]
    public void TrySkipAssignment_WithoutTaskOrBalance_DoesNotDebit()
    {
        var (noTask, noTaskMediator, _) = CreateSlayer(100, withTask: false);
        var (noBalance, noBalanceMediator, _) = CreateSlayer(null, withTask: true);

        Assert.IsFalse(noTask.TrySkipAssignment(30));
        Assert.IsFalse(noBalance.TrySkipAssignment(30));
        noTaskMediator.DidNotReceive().Publish(Arg.Any<Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions.ProfileDecrementIntAction>());
        noBalanceMediator.DidNotReceive().Publish(Arg.Any<Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions.ProfileDecrementIntAction>());
    }

    [TestMethod]
    public void TrySkipAssignment_RepeatedCall_ChargesOnceAndUnregistersKillHandler()
    {
        var (slayer, mediator, character) = CreateSlayer(100, withTask: true);

        Assert.IsTrue(slayer.TrySkipAssignment(30));
        Assert.IsFalse(slayer.TrySkipAssignment(30));

        mediator.Received(1).Publish(Arg.Any<Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions.ProfileDecrementIntAction>());
        character.Received(1).UnregisterEventHandler<CreatureKillEvent>(Arg.Any<EventHappened>());
    }

    [TestMethod]
    public void CreatureKillHandler_DoesNotWaitForPendingTaskDefinitionLookup()
    {
        var character = Substitute.For<ICharacter>();
        var slayerService = Substitute.For<ISlayerService>();
        var pendingLookup = new TaskCompletionSource<ISlayerTaskDefinition?>(TaskCreationOptions.RunContinuationsAsynchronously);
        slayerService.FindSlayerTaskDefinition(Arg.Any<int>()).Returns(pendingLookup.Task);
        var killHandler = default(EventHappened<CreatureKillEvent>);
        character.RegisterEventHandler(Arg.Do<EventHappened<CreatureKillEvent>>(handler => killHandler = handler));
        character.QueueTask(Arg.Any<Func<CancellationToken, Task>>())
            .Returns(Substitute.For<Hagalaz.Game.Abstractions.Tasks.IRsTaskHandle>());

        var slayer = new Slayer(
            character,
            slayerService,
            Substitute.For<IRatesService>(),
            Substitute.For<ISlayerTaskGenerator>(),
            Substitute.For<ISlayerTaskCompletedDialogue>(),
            Substitute.For<IGameMediator>());
        slayer.Hydrate(new HydratedSlayerDto
        {
            Task = new HydratedSlayerDto.SlayerTaskDto { Id = 1, KillCount = 10 }
        });

        var npc = Substitute.For<INpc>();
        killHandler!(new CreatureKillEvent(character, npc));

        Assert.IsFalse(pendingLookup.Task.IsCompleted);
        character.Received(1).QueueTask(Arg.Any<Func<CancellationToken, Task>>());
        pendingLookup.SetResult(null);
    }

    [TestMethod]
    public void CreatureKillTask_AwaitsCompletionRewardLookupWithoutBlocking()
    {
        var character = Substitute.For<ICharacter>();
        var statistics = Substitute.For<ICharacterStatistics>();
        character.Statistics.Returns(statistics);
        var slayerService = Substitute.For<ISlayerService>();
        var taskDefinition = Substitute.For<ISlayerTaskDefinition>();
        taskDefinition.CoinCount.Returns(0);
        taskDefinition.SlayerMasterId.Returns(7);
        slayerService.FindSlayerTaskDefinition(Arg.Any<int>()).Returns(Task.FromResult<ISlayerTaskDefinition?>(taskDefinition));
        var pendingMasterLookup = new TaskCompletionSource<ISlayerMasterTable?>(TaskCreationOptions.RunContinuationsAsynchronously);
        slayerService.FindSlayerMasterTableByNpcId(7).Returns(pendingMasterLookup.Task);
        var killHandler = default(EventHappened<CreatureKillEvent>);
        character.RegisterEventHandler(Arg.Do<EventHappened<CreatureKillEvent>>(handler => killHandler = handler));
        Hagalaz.Game.Abstractions.Tasks.ITaskItem? queuedTask = null;
        character.QueueTask(Arg.Do<Func<CancellationToken, Task>>(operation => queuedTask = new Hagalaz.Game.Abstractions.Tasks.RsAsyncTask(operation)))
            .Returns(Substitute.For<Hagalaz.Game.Abstractions.Tasks.IRsTaskHandle>());

        var slayer = new Slayer(
            character,
            slayerService,
            Substitute.For<IRatesService>(),
            Substitute.For<ISlayerTaskGenerator>(),
            Substitute.For<ISlayerTaskCompletedDialogue>(),
            Substitute.For<IGameMediator>());
        slayer.Hydrate(new HydratedSlayerDto
        {
            Task = new HydratedSlayerDto.SlayerTaskDto { Id = 1, KillCount = 1 }
        });

        var npc = Substitute.For<INpc>();
        var definition = Substitute.For<INpcDefinition>();
        definition.MaxLifePoints.Returns(100);
        npc.Definition.Returns(definition);
        killHandler!(new CreatureKillEvent(character, npc));
        Assert.IsNotNull(queuedTask);

        try
        {
            queuedTask!.Tick();
            Assert.IsFalse(pendingMasterLookup.Task.IsCompleted);
            Assert.AreEqual(1, slayer.CurrentKillCount);
        }
        finally
        {
            pendingMasterLookup.SetResult(null);
        }
    }
}
