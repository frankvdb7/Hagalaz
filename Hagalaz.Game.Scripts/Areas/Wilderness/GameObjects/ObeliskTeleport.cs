using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Areas.Wilderness.GameObjects
{
    /// <summary>
    /// </summary>
    /// <seealso cref="TeleportSpellScript" />
    public class ObeliskTeleport : TeleportSpellScript
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ObeliskTeleport" /> class.
        /// </summary>
        public ObeliskTeleport() => Book = MagicBook.StandardBook;

        /// <summary>
        ///     Determines whether this instance [can perform teleport] the specified caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        public override bool CanPerformTeleport(ICharacter caster)
        {
            if (caster.HasState(StateType.Teleporting))
            {
                return false;
            }

            if (!new TeleportAllowEvent(caster, Destination).Send())
            {
                return false;
            }

            return true;
        }
    }
}