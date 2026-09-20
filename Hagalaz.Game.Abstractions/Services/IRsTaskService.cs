using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that schedules and executes general game tasks.
    /// </summary>
    public interface IRsTaskService : IScheduler<ITaskItem>
    {
    }
}