using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CreatureTaskServiceTests
{
    [TestMethod]
    public void Queue_DelegatesToSharedSchedulerAndRuns()
    {
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CreatureTaskService(scheduler);
        var creature = Substitute.For<ICreature>();
        var executed = false;

        service.Queue(creature, new RsTask(() => executed = true, 1));
        scheduler.Tick();

        Assert.IsTrue(executed);
    }

    [TestMethod]
    public void Queue_AfterRevoke_CancelsWithoutScheduling()
    {
        var scheduler = Substitute.For<IRsTaskService>();
        var service = new CreatureTaskService(scheduler);
        var creature = Substitute.For<ICreature>();
        var task = new TrackingTask();

        service.Revoke(creature);
        service.Queue(creature, task);

        scheduler.DidNotReceive().Schedule(Arg.Any<ITaskItem>());
        Assert.AreEqual(1, task.CancelCalls);
    }

    [TestMethod]
    public void QueuedTask_StopsAfterRevoke()
    {
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CreatureTaskService(scheduler);
        var creature = Substitute.For<ICreature>();
        var ticks = 0;

        service.Queue(creature, new RsTickTask(() => ticks++));
        scheduler.Tick();
        service.Revoke(creature);
        scheduler.Tick();

        Assert.AreEqual(1, ticks);
    }

    [TestMethod]
    public void Queue_WhenCreatureIsReplaced_DoesNotInheritOldTasks()
    {
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CreatureTaskService(scheduler);
        var departing = Substitute.For<ICreature>();
        var replacement = Substitute.For<ICreature>();
        var departingTicks = 0;
        var replacementTicks = 0;

        service.Queue(departing, new RsTask(() => departingTicks++, 2));
        service.Revoke(departing);
        service.Queue(replacement, new RsTask(() => replacementTicks++, 1));
        scheduler.Tick();

        Assert.AreEqual(0, departingTicks);
        Assert.AreEqual(1, replacementTicks);
    }

    [TestMethod]
    public async Task QueueAndRevoke_ConcurrentAdmission_LeavesCreatureRevoked()
    {
        var scheduler = new GatedScheduler();
        var service = new CreatureTaskService(scheduler);
        var creature = Substitute.For<ICreature>();
        var task = new TrackingTask();

        var queue = Task.Run(() => service.Queue(creature, task));
        await scheduler.ScheduleEntered.Task;
        var revokeStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var revoke = Task.Run(() =>
        {
            revokeStarted.SetResult(true);
            service.Revoke(creature);
        });
        await revokeStarted.Task;

        scheduler.ReleaseSchedule.SetResult(true);
        await Task.WhenAll(queue, revoke);

        Assert.AreEqual(1, task.CancelCalls);
        var laterTask = new TrackingTask();
        service.Queue(creature, laterTask);
        Assert.AreEqual(1, laterTask.CancelCalls);
    }

    [TestMethod]
    public void CompletedTask_IsUntrackedBeforeRevoke()
    {
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CreatureTaskService(scheduler);
        var creature = Substitute.For<ICreature>();
        var task = new CompletingTask();

        service.Queue(creature, task);
        scheduler.Tick();
        service.Revoke(creature);

        Assert.AreEqual(0, task.CancelCalls);
    }

    private sealed class TrackingTask : ITaskItem
    {
        public int CancelCalls { get; private set; }
        public bool IsCancelled { get; private set; }
        public bool IsCompleted => false;
        public bool IsFaulted => false;
        public void Tick() { }
        public void Cancel()
        {
            CancelCalls++;
            IsCancelled = true;
        }
    }

    private sealed class CompletingTask : ITaskItem
    {
        public int CancelCalls { get; private set; }
        public bool IsCancelled { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsFaulted => false;
        public void Tick() => IsCompleted = true;
        public void Cancel()
        {
            CancelCalls++;
            IsCancelled = true;
        }
    }

    private sealed class GatedScheduler : IRsTaskService
    {
        public TaskCompletionSource<bool> ScheduleEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ReleaseSchedule { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Schedule(ITaskItem action)
        {
            ScheduleEntered.SetResult(true);
            ReleaseSchedule.Task.GetAwaiter().GetResult();
        }

        public void Tick() { }
    }
}
