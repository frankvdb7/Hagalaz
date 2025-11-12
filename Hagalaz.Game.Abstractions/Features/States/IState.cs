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
    }
}
