using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.States;

namespace Hagalaz.Game.Scripts.States
{
    /// <summary>
    /// </summary>
    [StateScriptMetaData(typeof(VengeanceState))]
    public class VengeanceState : StateScriptBase
    {
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
                character.SendChatMessage("Your vengeance has faded away.");
            }
        }

        /// <summary>
        ///     Determines whether this instance is serializable.
        ///     By default, the state is not serializable.
        /// </summary>
        /// <returns></returns>
        public override bool IsSerializable() => true; // yes we want it to save.
    }
}