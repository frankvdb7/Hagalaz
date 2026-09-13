using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the creature-owned boundary for scheduling and revoking tasks.
    /// </summary>
    public interface ICreatureTaskService
    {
        IRsTaskHandle Queue(ICreature creature, ITaskItem task);

        IRsTaskHandle<TResult> Queue<TResult>(ICreature creature, ITaskItem<TResult> task);

        void Revoke(ICreature creature);
    }
}
