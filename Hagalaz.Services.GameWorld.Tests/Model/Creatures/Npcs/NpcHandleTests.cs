using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Model.Creatures.Npcs;

[TestClass]
public sealed class NpcHandleTests
{
    [TestMethod]
    [Timeout(5000)]
    public async Task Unregister_WhenServiceIsPending_ReturnsWithoutBlockingAndCompletesOnLaterTick()
    {
        var npc = Substitute.For<INpc>();
        var npcService = Substitute.For<INpcService>();
        var queuedTasks = new List<ITaskItem>();
        var pendingUnregister = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        npc.When(item => item.QueueTask(Arg.Any<ITaskItem>()))
            .Do(callInfo => queuedTasks.Add(callInfo.Arg<ITaskItem>()!));
        npcService.UnregisterAsync(npc).Returns(pendingUnregister.Task);

        var handle = new NpcHandle(npc, npcService);

        var unregister = Task.Run(handle.Unregister);

        await unregister.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, queuedTasks.Count);
        npcService.DidNotReceive().UnregisterAsync(npc);

        var asynchronousUnregister = queuedTasks[0];
        var tick = Task.Run(asynchronousUnregister.Tick);
        await tick.WaitAsync(TimeSpan.FromSeconds(1));

        await npcService.Received(1).UnregisterAsync(npc);
        Assert.IsFalse(asynchronousUnregister.IsCompleted);

        pendingUnregister.TrySetResult();
        asynchronousUnregister.Tick();

        Assert.IsTrue(asynchronousUnregister.IsCompleted);
    }
}
