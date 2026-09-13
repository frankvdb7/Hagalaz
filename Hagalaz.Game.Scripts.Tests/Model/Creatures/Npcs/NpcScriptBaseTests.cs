using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Model.Creatures.Npcs;

[TestClass]
public sealed class NpcScriptBaseTests
{
    [TestMethod]
    public async Task Respawn_WhenNpcCannotSpawn_DoesNotBlockWhileUnregisterIsPending()
    {
        var npc = Substitute.For<INpc>();
        var npcService = Substitute.For<INpcService>();
        var queuedTasks = new List<ITaskItem>();
        var pendingUnregister = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        npc.When(item => item.QueueTask(Arg.Any<ITaskItem>()))
            .Do(callInfo => queuedTasks.Add(callInfo.Arg<ITaskItem>()!));
        npc.When(item => item.QueueTask(Arg.Any<Func<CancellationToken, Task>>()))
            .Do(callInfo => queuedTasks.Add(new RsAsyncTask(callInfo.Arg<Func<CancellationToken, Task>>()!)));
        npcService.UnregisterAsync(npc).Returns(pendingUnregister.Task);
        var script = new NonSpawningNpcScript(
            npc,
            npcService,
            Substitute.For<ISimplePathFinder>(),
            Substitute.For<IWidgetScriptActivator>());

        script.Respawn();
        Assert.AreEqual(1, queuedTasks.Count);

        queuedTasks[0].Tick();
        Assert.IsFalse(queuedTasks[0].IsCompleted);

        pendingUnregister.TrySetResult();
        queuedTasks[0].Tick();

        Assert.IsTrue(queuedTasks[0].IsCompleted);
        await npcService.Received(1).UnregisterAsync(npc);
    }

    private sealed class NonSpawningNpcScript(
        INpc owner,
        INpcService npcService,
        ISimplePathFinder pathFinder,
        IWidgetScriptActivator widgetScriptActivator)
        : NpcScriptBase(owner, npcService, pathFinder, widgetScriptActivator)
    {
        public override bool CanSpawn() => false;
    }
}
