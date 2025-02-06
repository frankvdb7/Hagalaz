using System;
using System.Collections.Generic;
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

        /// <summary>
        /// A queue containing all tasks to be processed.
        /// </summary>
        private readonly List<ITaskItem> _tasks = [];

        public RsTaskService(ILogger<RsTaskService> logger) => _logger = logger;

        /// <summary>
        /// Schedules the specified task.
        /// </summary>
        /// <param name="task">The task.</param>
        public void Schedule(ITaskItem task) => _tasks.Add(task);

        /// <summary>
        /// Ticks this instance.
        /// </summary>
        public void Tick()
        {
            for (var i = _tasks.Count - 1; i >= 0; i--)
            {
                if (_tasks[i].IsCancelled || _tasks[i].IsCompleted || _tasks[i].IsFaulted)
                {
                    _tasks.RemoveAt(i);
                    return;
                }

                try
                {
                    _tasks[i].Tick();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to tick task");
                }
            }
        }
    }
}