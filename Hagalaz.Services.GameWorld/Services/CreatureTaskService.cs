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
        private bool _lifetimeCancellationHandled;

        public bool IsCancelled => _lifetimeCancellationHandled || inner.IsCancelled;

        public bool IsCompleted => inner.IsCompleted;

        public bool IsFaulted => inner.IsFaulted;

        public void Tick()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                if (!_lifetimeCancellationHandled)
                {
                    _lifetimeCancellationHandled = true;
                    inner.Cancel();
                }

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
        private readonly CreatureTask _task = new(inner, cancellationToken);

        public bool IsCancelled => _task.IsCancelled;

        public bool IsCompleted => _task.IsCompleted;

        public bool IsFaulted => _task.IsFaulted;

        public void Tick() => _task.Tick();

        public void Cancel() => _task.Cancel();

        public void RegisterResultHandler(Action<TResult> resultHandler) => inner.RegisterResultHandler(resultHandler);

        public void Dispose() => _task.Dispose();
    }
}
