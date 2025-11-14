using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Defines the contract for a temporary state or effect that can be applied to a creature.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called when the state is applied to a creature.
        /// </summary>
        /// <param name="creature">The creature the state is being applied to.</param>
        void OnApply(ICreature creature);

        /// <summary>
        /// Called when the state is removed from a creature.
        /// </summary>
        /// <param name="creature">The creature the state is being removed from.</param>
        void OnRemove(ICreature creature);

        /// <summary>
        /// Called on each game tick while the state is active.
        /// </summary>
        /// <param name="creature">The creature the state is applied to.</param>
        void OnTick(ICreature creature);

        /// <summary>
        /// Gets or sets the number of ticks remaining for the state.
        /// </summary>
        int TicksLeft { get; set; }

        /// <summary>
        /// Gets the script associated with this state.
        /// </summary>
        IStateScript Script { get; }

        /// <summary>
        /// Gets a value indicating whether this state has been removed.
        /// </summary>
        bool Removed { get; }

        /// <summary>
        /// Gets the delay before this state is removed.
        /// </summary>
        int RemoveDelay { get; }

        /// <summary>
        /// Performs a tick update for the state.
        /// </summary>
        void Tick();
    }
}
