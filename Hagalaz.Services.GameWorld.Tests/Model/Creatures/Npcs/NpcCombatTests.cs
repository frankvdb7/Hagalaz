using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Extensions;
using Hagalaz.Services.GameWorld.Model.Creatures.Npcs;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Model.Creatures.Npcs;

[TestClass]
public sealed class NpcCombatTests
{
    [TestMethod]
    public async Task OnDeath_PermanentRemoval_DoesNotBlockWhileUnregisterIsPending()
    {
        var npc = Substitute.For<INpc>();
        var script = Substitute.For<INpcScript>();
        var definition = Substitute.For<INpcDefinition>();
        var appearance = Substitute.For<INpcAppearance>();
        var npcService = Substitute.For<INpcService>();
        var queuedTasks = new List<ITaskItem>();
        var unregisterStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingUnregister = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        script.CanRespawn().Returns(false);
        script.RenderDeath().Returns(0);
        npc.Script.Returns(script);
        npc.Definition.Returns(definition);
        npc.Appearance.Returns(appearance);
        npc.When(item => item.QueueTask(Arg.Any<ITaskItem>()))
            .Do(callInfo => queuedTasks.Add(callInfo.Arg<ITaskItem>()!));
        npcService.UnregisterAsync(npc).Returns(_ =>
        {
            unregisterStarted.SetResult();
            return pendingUnregister.Task;
        });

        var combat = new NpcCombat(
            npc,
            npcService,
            Substitute.For<ILootService>(),
            Substitute.For<ILootGenerator>(),
            Substitute.For<IGroundItemBuilder>(),
            Substitute.For<IProjectilePathFinder>(),
            Substitute.For<ISmartPathFinder>(),
            Options.Create(new CombatOptions()),
            Substitute.For<IHitSplatBuilder>());

        combat.OnDeath();

        Assert.AreEqual(2, queuedTasks.Count);
        var delayedRemoval = queuedTasks[1];
        var delayedTick = Task.Run(delayedRemoval.Tick);
        var delayedTickCompleted = false;
        try
        {
            await delayedTick.WaitAsync(TimeSpan.FromSeconds(1));
            delayedTickCompleted = true;
        }
        finally
        {
            if (!delayedTickCompleted)
                pendingUnregister.TrySetResult();
            await delayedTick;
        }

        Assert.AreEqual(3, queuedTasks.Count);
        Assert.IsFalse(unregisterStarted.Task.IsCompleted);

        var asynchronousRemoval = queuedTasks[2];
        asynchronousRemoval.Tick();

        await unregisterStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsFalse(asynchronousRemoval.IsCompleted);

        pendingUnregister.TrySetResult();
        asynchronousRemoval.Tick();

        Assert.IsTrue(asynchronousRemoval.IsCompleted);
        await npcService.Received(1).UnregisterAsync(npc);
    }
}
