using System.Threading;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the creature-owned boundary for scheduling cancellation-aware tasks.
    /// </summary>
    public interface ICreatureTaskService
    {
        IRsTaskHandle Queue(ITaskItem task, CancellationToken cancellationToken);

        IRsTaskHandle<TResult> Queue<TResult>(ITaskItem<TResult> task, CancellationToken cancellationToken);
    }
}
