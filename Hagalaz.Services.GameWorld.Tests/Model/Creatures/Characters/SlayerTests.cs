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
    [TestMethod]
    public void CreatureKillHandler_DoesNotWaitForPendingTaskDefinitionLookup()
    {
        var character = Substitute.For<ICharacter>();
        var slayerService = Substitute.For<ISlayerService>();
        var pendingLookup = new TaskCompletionSource<ISlayerTaskDefinition?>(TaskCreationOptions.RunContinuationsAsynchronously);
        slayerService.FindSlayerTaskDefinition(Arg.Any<int>()).Returns(pendingLookup.Task);
        var killHandler = default(EventHappened<CreatureKillEvent>);
        character.RegisterEventHandler(Arg.Do<EventHappened<CreatureKillEvent>>(handler => killHandler = handler));
        character.QueueTask(Arg.Any<Hagalaz.Game.Abstractions.Tasks.ITaskItem>()).Returns(Substitute.For<Hagalaz.Game.Abstractions.Tasks.IRsTaskHandle>());

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
        var invocation = Task.Run(() => killHandler!(new CreatureKillEvent(character, npc)));
        try
        {
            Assert.IsTrue(invocation.Wait(TimeSpan.FromSeconds(1)));
            Assert.IsFalse(pendingLookup.Task.IsCompleted);
            character.Received(1).QueueTask(Arg.Any<Hagalaz.Game.Abstractions.Tasks.ITaskItem>());
        }
        finally
        {
            pendingLookup.SetResult(null);
        }
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
        character.QueueTask(Arg.Do<Hagalaz.Game.Abstractions.Tasks.ITaskItem>(task => queuedTask = task))
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
            Assert.AreEqual(0, slayer.CurrentKillCount);
        }
        finally
        {
            pendingMasterLookup.SetResult(null);
        }
    }
}
