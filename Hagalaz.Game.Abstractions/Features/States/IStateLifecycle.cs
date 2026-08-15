using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Provides optional callbacks for real state lifecycle transitions.
    /// </summary>
    public interface IStateLifecycle : IState
    {
        /// <summary>
        /// Runs after the state becomes active on the creature.
        /// </summary>
        void OnAdded(ICreature creature);

        /// <summary>
        /// Runs after the state is removed from the creature.
        /// </summary>
        void OnRemoved(ICreature creature);
    }
}
