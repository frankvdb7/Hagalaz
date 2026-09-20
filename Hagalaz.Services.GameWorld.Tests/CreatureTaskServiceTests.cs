using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CreatureTaskServiceTests
{
    [TestMethod]
    public void Queue_DelegatesToSharedSchedulerAndRuns()
    {
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CreatureTaskService(scheduler);
        var executed = false;

        service.Queue(new RsTask(() => executed = true, 1), CancellationToken.None);
        scheduler.Tick();

        Assert.IsTrue(executed);
    }

    [TestMethod]
    public void Queue_WithAlreadyCancelledToken_SchedulesCancellationCleanup()
    {
        var scheduler = new RecordingScheduler();
        var service = new CreatureTaskService(scheduler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var task = new TrackingTask();

        service.Queue(task, cancellation.Token);

        Assert.AreEqual(1, scheduler.ScheduleCalls);
        scheduler.Tick();
        Assert.AreEqual(1, task.CancelCalls);
    }

    [TestMethod]
    public void QueuedTask_StopsAfterTokenCancellation()
    {
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CreatureTaskService(scheduler);
        using var cancellation = new CancellationTokenSource();
        var ticks = 0;

        service.Queue(new RsTickTask(() => ticks++), cancellation.Token);
        scheduler.Tick();
        cancellation.Cancel();
        scheduler.Tick();

        Assert.AreEqual(1, ticks);
    }

    [TestMethod]
    public void QueuedTask_WithIgnoringCancellation_IsRemovedAndDisposedByScheduler()
    {
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CreatureTaskService(scheduler);
        using var cancellation = new CancellationTokenSource();
        var task = new IgnoringCancellationTask();

        service.Queue(task, cancellation.Token);
        scheduler.Tick();
        cancellation.Cancel();

        scheduler.Tick();

        Assert.AreEqual(1, task.CancelCalls);
        Assert.IsFalse(task.IsDisposed);
        Assert.HasCount(1, scheduler.Tasks);

        scheduler.Tick();

        Assert.IsTrue(task.IsDisposed);
        Assert.IsEmpty(scheduler.Tasks);
    }

    [TestMethod]
    public void QueuedRsAsyncTask_IsRemovedWhenOperationIgnoresCancellationAndCompletes()
    {
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CreatureTaskService(scheduler);
        using var cancellation = new CancellationTokenSource();
        var operation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = new RsAsyncTask(_ => operation.Task);

        service.Queue(task, cancellation.Token);
        scheduler.Tick();
        cancellation.Cancel();
        scheduler.Tick();

        operation.SetResult(true);
        scheduler.Tick();

        Assert.IsEmpty(scheduler.Tasks);
    }

    [TestMethod]
    public void Queue_GenericTaskPreservesResultHandling()
    {
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CreatureTaskService(scheduler);
        int? result = null;

        service.Queue(new RsTask<int>(() => 42, 1), CancellationToken.None)
            .RegisterResultHandler(value => result = value);
        scheduler.Tick();

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public async Task Queue_CancellationAwareAsyncTaskReceivesTokenCancellation()
    {
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var service = new CreatureTaskService(scheduler);
        using var cancellation = new CancellationTokenSource();
        var operationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedToken = CancellationToken.None;
        var task = new RsAsyncTask(token =>
        {
            receivedToken = token;
            operationStarted.SetResult(true);
            return operation.Task;
        }, cancellation.Token);

        service.Queue(task, cancellation.Token);
        scheduler.Tick();
        await operationStarted.Task;

        cancellation.Cancel();

        Assert.IsTrue(receivedToken.IsCancellationRequested);
        operation.SetResult(true);
        scheduler.Tick();
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

    private sealed class IgnoringCancellationTask : ITaskItem, IDisposable
    {
        public int CancelCalls { get; private set; }
        public bool IsCancelled => false;
        public bool IsCompleted => false;
        public bool IsFaulted => false;
        public bool IsDisposed { get; private set; }

        public void Tick() { }

        public void Cancel() => CancelCalls++;

        public void Dispose() => IsDisposed = true;
    }

    private sealed class RecordingScheduler : IRsTaskService
    {
        public int ScheduleCalls { get; private set; }
        private ITaskItem? _scheduled;

        public void Schedule(ITaskItem action)
        {
            ScheduleCalls++;
            _scheduled = action;
        }

        public void Tick() => (_scheduled ?? throw new InvalidOperationException("No task was scheduled.")).Tick();
        public void ScheduleLifecycleCritical(System.Action action) => action();
        public void BeginShutdown() { }
        public void CompleteShutdown() { }
    }
}
