using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Services.GameWorld.Services;

/// <summary>
/// Owns admission of deferred character gameplay at the GameWorld boundary.
/// </summary>
public sealed class CharacterExecutionService(
    ICharacterStore characterStore,
    CharacterLogoutState logoutState,
    IRsTaskService taskService) : ICharacterExecutionService
{
    private readonly object _gate = new();
    private readonly List<(ICharacter Character, ITaskItem Task)> _admittedTasks = [];

    public IRsTaskHandle Queue(ICharacter expectedCharacter, ITaskItem task)
    {
        ArgumentNullException.ThrowIfNull(expectedCharacter);
        ArgumentNullException.ThrowIfNull(task);

        var ownedTask = new CharacterTask(expectedCharacter, task, IsOwned, Untrack);
        if (!TryAdmit(expectedCharacter, ownedTask))
        {
            task.Cancel();
        }

        return new RsTaskHandle(ownedTask);
    }

    public IRsTaskHandle<TResult> Queue<TResult>(ICharacter expectedCharacter, ITaskItem<TResult> task)
    {
        ArgumentNullException.ThrowIfNull(expectedCharacter);
        ArgumentNullException.ThrowIfNull(task);

        var ownedTask = new CharacterTask<TResult>(expectedCharacter, task, IsOwned, Untrack);
        if (!TryAdmit(expectedCharacter, ownedTask))
        {
            task.Cancel();
        }

        return new RsTaskHandle<TResult>(ownedTask);
    }

    public void Revoke(ICharacter expectedCharacter)
    {
        ArgumentNullException.ThrowIfNull(expectedCharacter);

        ITaskItem[] tasks;
        lock (_gate)
        {
            tasks = _admittedTasks
                .Where(entry => ReferenceEquals(entry.Character, expectedCharacter))
                .Select(entry => entry.Task)
                .ToArray();
        }

        foreach (var task in tasks)
        {
            task.Cancel();
        }
    }

    private bool TryAdmit(ICharacter expectedCharacter, ITaskItem task)
    {
        return logoutState.TryAdmit(expectedCharacter, () =>
        {
            if (!characterStore.IsCurrent(expectedCharacter))
            {
                return false;
            }

            Track(expectedCharacter, task);
            try
            {
                taskService.Schedule(task);
            }
            catch
            {
                Untrack(task);
                throw;
            }

            return true;
        });
    }

    private bool IsOwned(ICharacter expectedCharacter) => characterStore.IsCurrent(expectedCharacter);

    private void Track(ICharacter character, ITaskItem task)
    {
        lock (_gate)
        {
            _admittedTasks.Add((character, task));
        }
    }

    private void Untrack(ITaskItem task)
    {
        lock (_gate)
        {
            _admittedTasks.RemoveAll(entry => ReferenceEquals(entry.Task, task));
        }
    }

    private sealed class CharacterTask(
        ICharacter expectedCharacter,
        ITaskItem inner,
        Func<ICharacter, bool> isOwned,
        Action<ITaskItem> onTerminated) : ITaskItem, IDisposable
    {
        public bool IsCancelled => inner.IsCancelled;
        public bool IsCompleted => inner.IsCompleted;
        public bool IsFaulted => inner.IsFaulted;

        public void Tick()
        {
            if (!isOwned(expectedCharacter))
            {
                inner.Cancel();
                onTerminated(this);
                return;
            }

            try
            {
                inner.Tick();
            }
            finally
            {
                if (inner.IsCancelled || inner.IsCompleted || inner.IsFaulted)
                {
                    onTerminated(this);
                }
            }
        }

        public void Cancel()
        {
            inner.Cancel();
            onTerminated(this);
        }

        public void Dispose()
        {
            onTerminated(this);
            if (inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private sealed class CharacterTask<TResult>(
        ICharacter expectedCharacter,
        ITaskItem<TResult> inner,
        Func<ICharacter, bool> isOwned,
        Action<ITaskItem> onTerminated) : ITaskItem<TResult>, IDisposable
    {
        public bool IsCancelled => inner.IsCancelled;
        public bool IsCompleted => inner.IsCompleted;
        public bool IsFaulted => inner.IsFaulted;

        public void Tick()
        {
            if (!isOwned(expectedCharacter))
            {
                inner.Cancel();
                onTerminated(this);
                return;
            }

            try
            {
                inner.Tick();
            }
            finally
            {
                if (inner.IsCancelled || inner.IsCompleted || inner.IsFaulted)
                {
                    onTerminated(this);
                }
            }
        }

        public void Cancel()
        {
            inner.Cancel();
            onTerminated(this);
        }

        public void RegisterResultHandler(Action<TResult> resultHandler) => inner.RegisterResultHandler(resultHandler);

        public void Dispose()
        {
            onTerminated(this);
            if (inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
