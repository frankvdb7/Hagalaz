using System;
using System.Threading;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Services.GameWorld.Services;

/// <summary>
/// Adds creature-lifetime cancellation semantics while delegating execution to the shared game scheduler.
/// </summary>
public sealed class CreatureTaskService(IRsTaskService scheduler) : ICreatureTaskService
{
    public IRsTaskHandle Queue(ITaskItem task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        var wrappedTask = new CreatureTask(task, cancellationToken);
        scheduler.Schedule(wrappedTask);
        return new RsTaskHandle(wrappedTask);
    }

    public IRsTaskHandle<TResult> Queue<TResult>(ITaskItem<TResult> task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        var wrappedTask = new CreatureTask<TResult>(task, cancellationToken);
        scheduler.Schedule(wrappedTask);
        return new RsTaskHandle<TResult>(wrappedTask);
    }

    private sealed class CreatureTask(ITaskItem inner, CancellationToken cancellationToken) : ITaskItem, IDisposable
    {
        public bool IsCancelled => inner.IsCancelled;

        public bool IsCompleted => inner.IsCompleted;

        public bool IsFaulted => inner.IsFaulted;

        public void Tick()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                inner.Cancel();
                return;
            }

            inner.Tick();
        }

        public void Cancel() => inner.Cancel();

        public void Dispose()
        {
            if (inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private sealed class CreatureTask<TResult>(ITaskItem<TResult> inner, CancellationToken cancellationToken) : ITaskItem<TResult>, IDisposable
    {
        public bool IsCancelled => inner.IsCancelled;

        public bool IsCompleted => inner.IsCompleted;

        public bool IsFaulted => inner.IsFaulted;

        public void Tick()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                inner.Cancel();
                return;
            }

            inner.Tick();
        }

        public void Cancel() => inner.Cancel();

        public void RegisterResultHandler(Action<TResult> resultHandler) => inner.RegisterResultHandler(resultHandler);

        public void Dispose()
        {
            if (inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
