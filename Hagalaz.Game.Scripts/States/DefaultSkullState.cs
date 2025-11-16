using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.States;

namespace Hagalaz.Game.Scripts.States
{
    /// <summary>
    /// </summary>
    [StateId("default-skull")]
    public class DefaultSkullState : StateBase
    {
        /// <summary>
        ///     Determines whether this instance is serializable.
        ///     By default, the state is not serializable.
        /// </summary>
        /// <returns></returns>
        public override bool IsSerializable() => true; // yes we want it to save.

        /// <summary>
        ///     Gets called when the state is added.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public override void OnStateAdded(IState state, ICreature creature)
        {
            if (creature is ICharacter character)
            {
                character.Appearance.SkullIcon = SkullIcon.DefaultSkull;
            }
        }

        /// <summary>
        ///     Gets called when the state is removed.
        ///     By default, this method invokes the OnRemoveCallback of the state, if any.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public override void OnStateRemoved(IState state, ICreature creature)
        {
            if (creature is ICharacter character)
            {
                character.Appearance.SkullIcon = SkullIcon.None;
            }
        }
    }
}