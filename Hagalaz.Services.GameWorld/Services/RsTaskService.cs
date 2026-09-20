using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class RsTaskService : IRsTaskService
    {
        private readonly ILogger<RsTaskService> _logger;
        private readonly GameLoopSynchronizationContext _synchronizationContext = new();

        /// <summary>
        /// A queue containing all tasks to be processed.
        /// </summary>
        private readonly List<ITaskItem> _tasks = [];
        private readonly ConcurrentQueue<ITaskItem> _pendingTasks = new();
        private readonly object _lifecycleGate = new();
        private readonly object _lifecycleExecutionGate = new();
        private readonly HashSet<RsTask> _lifecycleTasks = [];
        private LifecycleState _lifecycleState = LifecycleState.Running;

        internal IReadOnlyList<ITaskItem> Tasks => _tasks;

        public RsTaskService(ILogger<RsTaskService> logger) => _logger = logger;

        /// <summary>
        /// Schedules the specified task.
        /// </summary>
        /// <param name="task">The task.</param>
        public void Schedule(ITaskItem task) => _pendingTasks.Enqueue(task);

        public void ScheduleLifecycleCritical(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            var task = new RsTask(action, 1);
            var executeImmediately = false;
            lock (_lifecycleGate)
            {
                if (_lifecycleState == LifecycleState.Stopped)
                {
                    executeImmediately = true;
                }
                else
                {
                    _lifecycleTasks.Add(task);
                    _pendingTasks.Enqueue(task);
                }
            }

            if (executeImmediately)
            {
                ExecuteLifecycleTask(task);
            }
        }

        public void BeginShutdown()
        {
            lock (_lifecycleGate)
            {
                if (_lifecycleState == LifecycleState.Running)
                {
                    _lifecycleState = LifecycleState.Stopping;
                }
            }
        }

        public void CompleteShutdown()
        {
            while (true)
            {
                RsTask[] lifecycleTasks;
                lock (_lifecycleGate)
                {
                    if (_lifecycleTasks.Count == 0)
                    {
                        _lifecycleState = LifecycleState.Stopped;
                        return;
                    }

                    _lifecycleState = LifecycleState.Draining;
                    lifecycleTasks = [.. _lifecycleTasks];
                    _lifecycleTasks.Clear();
                }

                foreach (var task in lifecycleTasks)
                {
                    ExecuteLifecycleTask(task);
                }
            }
        }

        /// <summary>
        /// Ticks this instance.
        /// </summary>
        public void Tick()
        {
            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);

            try
            {
                while (_pendingTasks.TryDequeue(out var pendingTask))
                {
                    _tasks.Add(pendingTask);
                }

                _synchronizationContext.RunPending();

                for (var i = 0; i < _tasks.Count;)
                {
                    var task = _tasks[i];
                    if (task.IsCancelled || task.IsCompleted || task.IsFaulted)
                    {
                        _tasks.RemoveAt(i);
                        RemoveLifecycleTask(task);
                        if (task is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }

                        continue;
                    }

                    try
                    {
                        task.Tick();
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger.LogDebug(ex, "Task was canceled while ticking");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Failed to tick task");
                    }

                    if (task.IsCancelled || task.IsCompleted || task.IsFaulted)
                    {
                        RemoveLifecycleTask(task);
                    }

                    i++;
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        private void RemoveLifecycleTask(ITaskItem task)
        {
            if (task is not RsTask rsTask)
            {
                return;
            }

            lock (_lifecycleGate)
            {
                _lifecycleTasks.Remove(rsTask);
            }
        }

        private void ExecuteLifecycleTask(RsTask task)
        {
            lock (_lifecycleExecutionGate)
            {
                var previousContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
                try
                {
                    while (!task.IsCancelled && !task.IsCompleted && !task.IsFaulted)
                    {
                        task.Tick();
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogDebug(ex, "Lifecycle task was canceled while completing scheduler shutdown");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to complete lifecycle task during scheduler shutdown");
                }
                finally
                {
                    try
                    {
                        task.Dispose();
                    }
                    finally
                    {
                        SynchronizationContext.SetSynchronizationContext(previousContext);
                    }
                }
            }
        }

        private enum LifecycleState
        {
            Running,
            Stopping,
            Draining,
            Stopped
        }
    }
}
