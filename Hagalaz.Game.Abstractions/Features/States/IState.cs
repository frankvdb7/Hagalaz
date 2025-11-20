using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Defines the contract for a temporary state or effect that can be applied to a creature.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Gets or sets the number of ticks remaining for the state.
        /// </summary>
        int TicksLeft { get; set; }

        /// <summary>
        /// Performs a tick update for the state.
        /// </summary>
        void Tick();

        /// <summary>
        /// A callback method that is executed when the state is removed from a creature (e.g., when it expires or is cured).
        /// </summary>
        /// <param name="state">The state instance that is being removed.</param>
        /// <param name="creature">The creature from which the state is being removed.</param>
        void OnStateRemoved(IState state, ICreature creature);

        /// <summary>
        /// A callback method that is executed when the state is first added to a creature.
        /// </summary>
        /// <param name="state">The state instance that has been added.</param>
        /// <param name="creature">The creature to which the state has been added.</param>
        void OnStateAdded(IState state, ICreature creature);
    }
}
