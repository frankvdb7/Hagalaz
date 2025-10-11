using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// Defines the contract for a script that contains the logic for a temporary state or effect.
    /// </summary>
    public interface IStateScript
    {
        /// <summary>
        /// Determines whether the state associated with this script should be saved when the character logs out.
        /// </summary>
        /// <returns><c>true</c> if the state is serializable and should persist; otherwise, <c>false</c>.</returns>
        bool IsSerializable();

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
