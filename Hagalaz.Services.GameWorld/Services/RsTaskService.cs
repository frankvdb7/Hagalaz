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
    public class RsTaskService : IRsTaskService, ICreatureTaskService
    {
        private readonly ILogger<RsTaskService> _logger;
        private readonly GameLoopSynchronizationContext _synchronizationContext = new();

        /// <summary>
        /// A queue containing all tasks to be processed.
        /// </summary>
        private readonly List<ITaskItem> _tasks = [];
        private readonly ConcurrentQueue<ITaskItem> _pendingTasks = new();

        internal IReadOnlyList<ITaskItem> Tasks => _tasks;

        public RsTaskService(ILogger<RsTaskService> logger) => _logger = logger;

        /// <summary>
        /// Schedules the specified task.
        /// </summary>
        /// <param name="task">The task.</param>
        public void Schedule(ITaskItem task) => _pendingTasks.Enqueue(task);

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

                for (var i = _tasks.Count - 1; i >= 0; i--)
                {
                    if (_tasks[i].IsCancelled || _tasks[i].IsCompleted || _tasks[i].IsFaulted)
                    {
                        _tasks.RemoveAt(i);
                        continue;
                    }

                    try
                    {
                        _tasks[i].Tick();
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger.LogDebug(ex, "Task was canceled while ticking");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Failed to tick task");
                    }
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }
    }
}
