using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that schedules and executes tasks related to creatures.
    /// </summary>
    public interface ICreatureTaskService : IScheduler<ITaskItem>
    {
    }
}