using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common.Events;
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
        var (slayer, _, character) = CreateSlayer(balance, withTask: true);

        Assert.AreEqual(expected, slayer.TrySkipAssignment(30));
        Assert.AreEqual(expected ? -1 : 1, slayer.CurrentTaskId);
        character.Profile.Received(expected ? 1 : 0).SetValue(Hagalaz.Configuration.ProfileConstants.SlayerRewardPoints, balance - 30);
    }

    [TestMethod]
    public void TrySkipAssignment_WithoutTaskOrBalance_DoesNotDebit()
    {
        var (noTask, _, noTaskCharacter) = CreateSlayer(100, withTask: false);
        var (noBalance, _, noBalanceCharacter) = CreateSlayer(null, withTask: true);

        Assert.IsFalse(noTask.TrySkipAssignment(30));
        Assert.IsFalse(noBalance.TrySkipAssignment(30));
        noTaskCharacter.Profile.DidNotReceiveWithAnyArgs().SetValue<int>(default!, default);
        noBalanceCharacter.Profile.DidNotReceiveWithAnyArgs().SetValue<int>(default!, default);
    }

    [TestMethod]
    public void TrySkipAssignment_RepeatedCall_ChargesOnceAndUnregistersKillHandler()
    {
        var (slayer, _, character) = CreateSlayer(100, withTask: true);

        Assert.IsTrue(slayer.TrySkipAssignment(30));
        Assert.IsFalse(slayer.TrySkipAssignment(30));

        character.Profile.Received(1).SetValue(Hagalaz.Configuration.ProfileConstants.SlayerRewardPoints, 70);
        character.Received(1).UnregisterEventHandler<CreatureKillEvent>(Arg.Any<EventHappened>());
    }

    [TestMethod]
    public void TrySkipAssignment_WithRealProfile_PersistsDebitAndClearedTaskAcrossRelogin()
    {
        var character = Substitute.For<ICharacter>();
        var profile = new Profile();
        profile.SetValue(Hagalaz.Configuration.ProfileConstants.SlayerRewardPoints, 45);
        character.Profile.Returns(profile);
        Slayer Create() => new(
            character,
            Substitute.For<ISlayerService>(),
            Substitute.For<IRatesService>(),
            Substitute.For<ISlayerTaskGenerator>(),
            Substitute.For<ISlayerTaskCompletedDialogue>(),
            Substitute.For<IGameMediator>());
        var slayer = Create();
        slayer.Hydrate(new HydratedSlayerDto { Task = new HydratedSlayerDto.SlayerTaskDto { Id = 1, KillCount = 10 } });

        Assert.IsTrue(slayer.TrySkipAssignment(30));
        Assert.IsFalse(slayer.TrySkipAssignment(30));

        var reloadedProfile = new Profile();
        reloadedProfile.Hydrate(profile.Dehydrate());
        var reloadedSlayer = Create();
        reloadedSlayer.Hydrate(slayer.Dehydrate());
        Assert.AreEqual(15, reloadedProfile.GetValue<int>(Hagalaz.Configuration.ProfileConstants.SlayerRewardPoints));
        Assert.AreEqual(-1, reloadedSlayer.CurrentTaskId);
    }

    [TestMethod]
    public void PausedKillLookup_AfterSkipAndSameIdReassignment_DoesNotTouchNewAssignment()
    {
        var character = Substitute.For<ICharacter>();
        var statistics = Substitute.For<ICharacterStatistics>();
        character.Statistics.Returns(statistics);
        var profile = new Profile();
        profile.SetValue(Hagalaz.Configuration.ProfileConstants.SlayerRewardPoints, 100);
        character.Profile.Returns(profile);
        var queued = new System.Collections.Generic.List<Hagalaz.Game.Abstractions.Tasks.ITaskItem>();
        character.QueueTask(Arg.Do<Func<CancellationToken, Task>>(op => queued.Add(new Hagalaz.Game.Abstractions.Tasks.RsAsyncTask(op))))
            .Returns(Substitute.For<Hagalaz.Game.Abstractions.Tasks.IRsTaskHandle>());
        var killHandler = default(EventHappened<CreatureKillEvent>);
        character.RegisterEventHandler(Arg.Do<EventHappened<CreatureKillEvent>>(handler => killHandler = handler));

        var definition = Substitute.For<ISlayerTaskDefinition>();
        definition.Id.Returns(1);
        definition.CoinCount.Returns(0);
        definition.SlayerMasterId.Returns(7);
        definition.NpcIds.Returns([100]);
        var pendingLookup = new TaskCompletionSource<ISlayerTaskDefinition?>();
        var slayerService = Substitute.For<ISlayerService>();
        slayerService.FindSlayerTaskDefinition(1).Returns(pendingLookup.Task);
        slayerService.FindSlayerMasterTableByNpcId(7).Returns(Task.FromResult<ISlayerMasterTable?>(Substitute.For<ISlayerMasterTable>()));
        var generator = Substitute.For<ISlayerTaskGenerator>();
        generator.GenerateTask(Arg.Any<SlayerTaskParams>()).Returns([new SlayerTaskResult(definition, 5)]);

        var slayer = new Slayer(
            character,
            slayerService,
            Substitute.For<IRatesService>(),
            generator,
            Substitute.For<ISlayerTaskCompletedDialogue>(),
            Substitute.For<IGameMediator>());
        slayer.Hydrate(new HydratedSlayerDto { Task = new HydratedSlayerDto.SlayerTaskDto { Id = 1, KillCount = 1 } });

        var npc = Substitute.For<INpc>();
        var npcDefinition = Substitute.For<INpcDefinition>();
        npcDefinition.Id.Returns(100);
        npcDefinition.MaxLifePoints.Returns(100);
        npc.Definition.Returns(npcDefinition);
        killHandler!(new CreatureKillEvent(character, npc));
        queued[0].Tick(); // old kill is now paused inside the definition lookup

        Assert.IsTrue(slayer.TrySkipAssignment(30));
        slayer.AssignNewTask(7);
        queued[1].Tick();
        Assert.AreEqual(1, slayer.CurrentTaskId);
        Assert.AreEqual(5, slayer.CurrentKillCount);

        pendingLookup.SetResult(definition);

        Assert.AreEqual(5, slayer.CurrentKillCount);
        Assert.AreEqual(1, slayer.CurrentTaskId);
        statistics.DidNotReceiveWithAnyArgs().AddExperience(default, default);
        character.Widgets.DidNotReceiveWithAnyArgs().OpenDialogue(default!, default);
    }

    [TestMethod]
    public void CreatureKill_OnlyAssignedTargetsAdvanceTask()
    {
        var character = Substitute.For<ICharacter>();
        var statistics = Substitute.For<ICharacterStatistics>();
        character.Statistics.Returns(statistics);
        var queued = new System.Collections.Generic.List<Hagalaz.Game.Abstractions.Tasks.ITaskItem>();
        character.QueueTask(Arg.Do<Func<CancellationToken, Task>>(op => queued.Add(new Hagalaz.Game.Abstractions.Tasks.RsAsyncTask(op))))
            .Returns(Substitute.For<Hagalaz.Game.Abstractions.Tasks.IRsTaskHandle>());
        var killHandler = default(EventHappened<CreatureKillEvent>);
        character.RegisterEventHandler(Arg.Do<EventHappened<CreatureKillEvent>>(handler => killHandler = handler));

        var definition = Substitute.For<ISlayerTaskDefinition>();
        definition.Id.Returns(1);
        definition.SlayerMasterId.Returns(7);
        definition.NpcIds.Returns([100, 101]);
        var masterTable = Substitute.For<ISlayerMasterTable>();
        var slayerService = Substitute.For<ISlayerService>();
        slayerService.FindSlayerTaskDefinition(1).Returns(Task.FromResult<ISlayerTaskDefinition?>(definition));
        slayerService.FindSlayerMasterTableByNpcId(7).Returns(Task.FromResult<ISlayerMasterTable?>(masterTable));

        var slayer = new Slayer(
            character,
            slayerService,
            Substitute.For<IRatesService>(),
            Substitute.For<ISlayerTaskGenerator>(),
            Substitute.For<ISlayerTaskCompletedDialogue>(),
            Substitute.For<IGameMediator>());
        slayer.Hydrate(new HydratedSlayerDto { Task = new HydratedSlayerDto.SlayerTaskDto { Id = 1, KillCount = 3 } });

        INpc Kill(int npcId)
        {
            var npc = Substitute.For<INpc>();
            var npcDefinition = Substitute.For<INpcDefinition>();
            npcDefinition.Id.Returns(npcId);
            npcDefinition.MaxLifePoints.Returns(100);
            npc.Definition.Returns(npcDefinition);
            killHandler!(new CreatureKillEvent(character, npc));
            queued[^1].Tick();
            return npc;
        }

        Kill(999);
        Assert.AreEqual(3, slayer.CurrentKillCount);
        statistics.DidNotReceiveWithAnyArgs().AddExperience(default, default);

        Kill(100);
        Kill(101);
        Assert.AreEqual(1, slayer.CurrentKillCount);
        statistics.Received(2).AddExperience(Arg.Any<int>(), 10.0);

        Kill(999);
        Assert.AreEqual(1, slayer.CurrentKillCount);
        Assert.AreEqual(1, slayer.CurrentTaskId);

        Kill(100);
        Assert.AreEqual(-1, slayer.CurrentTaskId);
        character.Widgets.Received(1).OpenDialogue(Arg.Any<ISlayerTaskCompletedDialogue>(), true);
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
        taskDefinition.NpcIds.Returns([100]);
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
        definition.Id.Returns(100);
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
