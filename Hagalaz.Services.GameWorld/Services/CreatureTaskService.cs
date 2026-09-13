using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Services.GameWorld.Services;

/// <summary>
/// Owns the tasks queued for each creature while delegating execution to the shared game scheduler.
/// </summary>
public sealed class CreatureTaskService(IRsTaskService scheduler) : ICreatureTaskService
{
    private readonly object _gate = new();
    private readonly ConditionalWeakTable<ICreature, CreatureTaskState> _states = new();

    public IRsTaskHandle Queue(ICreature creature, ITaskItem task)
    {
        ArgumentNullException.ThrowIfNull(creature);
        ArgumentNullException.ThrowIfNull(task);

        var wrappedTask = new CreatureTask(creature, task, Untrack);
        lock (_gate)
        {
            var state = _states.GetValue(creature, static _ => new CreatureTaskState());
            if (state.Revoked)
            {
                wrappedTask.Cancel();
                return new RsTaskHandle(wrappedTask);
            }

            state.Tasks.Add(wrappedTask);
            try
            {
                scheduler.Schedule(wrappedTask);
            }
            catch
            {
                state.Tasks.Remove(wrappedTask);
                wrappedTask.Cancel();
                throw;
            }
        }

        return new RsTaskHandle(wrappedTask);
    }

    public IRsTaskHandle<TResult> Queue<TResult>(ICreature creature, ITaskItem<TResult> task)
    {
        ArgumentNullException.ThrowIfNull(creature);
        ArgumentNullException.ThrowIfNull(task);

        var wrappedTask = new CreatureTask<TResult>(creature, task, Untrack);
        lock (_gate)
        {
            var state = _states.GetValue(creature, static _ => new CreatureTaskState());
            if (state.Revoked)
            {
                wrappedTask.Cancel();
                return new RsTaskHandle<TResult>(wrappedTask);
            }

            state.Tasks.Add(wrappedTask);
            try
            {
                scheduler.Schedule(wrappedTask);
            }
            catch
            {
                state.Tasks.Remove(wrappedTask);
                wrappedTask.Cancel();
                throw;
            }
        }

        return new RsTaskHandle<TResult>(wrappedTask);
    }

    public void Revoke(ICreature creature)
    {
        ArgumentNullException.ThrowIfNull(creature);

        ITaskItem[] tasks;
        lock (_gate)
        {
            var state = _states.GetValue(creature, static _ => new CreatureTaskState());
            state.Revoked = true;
            tasks = [.. state.Tasks];
            state.Tasks.Clear();
        }

        foreach (var task in tasks)
        {
            task.Cancel();
        }
    }

    private void Untrack(ICreature creature, ITaskItem task)
    {
        lock (_gate)
        {
            if (!_states.TryGetValue(creature, out var state))
            {
                return;
            }

            state.Tasks.Remove(task);
            if (!state.Revoked && state.Tasks.Count == 0)
            {
                _states.Remove(creature);
            }
        }
    }

    private sealed class CreatureTaskState
    {
        public bool Revoked { get; set; }

        public HashSet<ITaskItem> Tasks { get; } = [];
    }

    private sealed class CreatureTask(
        ICreature creature,
        ITaskItem inner,
        Action<ICreature, ITaskItem> untrack) : ITaskItem, IDisposable
    {
        public bool IsCancelled => inner.IsCancelled;

        public bool IsCompleted => inner.IsCompleted;

        public bool IsFaulted => inner.IsFaulted;

        public void Tick()
        {
            try
            {
                inner.Tick();
            }
            finally
            {
                if (inner.IsCancelled || inner.IsCompleted || inner.IsFaulted)
                {
                    untrack(creature, this);
                }
            }
        }

        public void Cancel()
        {
            try
            {
                inner.Cancel();
            }
            finally
            {
                untrack(creature, this);
            }
        }

        public void Dispose()
        {
            untrack(creature, this);
            if (inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private sealed class CreatureTask<TResult>(
        ICreature creature,
        ITaskItem<TResult> inner,
        Action<ICreature, ITaskItem> untrack) : ITaskItem<TResult>, IDisposable
    {
        public bool IsCancelled => inner.IsCancelled;

        public bool IsCompleted => inner.IsCompleted;

        public bool IsFaulted => inner.IsFaulted;

        public void Tick()
        {
            try
            {
                inner.Tick();
            }
            finally
            {
                if (inner.IsCancelled || inner.IsCompleted || inner.IsFaulted)
                {
                    untrack(creature, this);
                }
            }
        }

        public void Cancel()
        {
            try
            {
                inner.Cancel();
            }
            finally
            {
                untrack(creature, this);
            }
        }

        public void RegisterResultHandler(Action<TResult> resultHandler) => inner.RegisterResultHandler(resultHandler);

        public void Dispose()
        {
            untrack(creature, this);
            if (inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
