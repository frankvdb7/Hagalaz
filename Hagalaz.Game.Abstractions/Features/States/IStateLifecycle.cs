using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Provides an optional callback for state removal.
    /// </summary>
    public interface IStateLifecycle : IState
    {
        /// <summary>
        /// Runs after the state is removed from the creature.
        /// </summary>
        void OnRemoved(ICreature creature);
    }
}
