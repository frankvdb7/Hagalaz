using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells
{
    /// <summary>
    ///     Contains home teleport spell.
    /// </summary>
    public class HomeTeleport : TeleportSpellScript
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="HomeTeleport" /> class.
        /// </summary>
        /// <param name="destination">The destination.</param>
        public HomeTeleport(ILocation destination) => Destination = destination;

        /// <summary>
        ///     Performs the teleport.
        /// </summary>
        /// <param name="caster"></param>
        public override void PerformTeleport(ICharacter caster)
        {
            if (!CanPerformTeleport(caster))
            {
                return;
            }

            caster.Interrupt(this);
            caster.Movement.Lock(true);
            caster.QueueTask(new HomeTeleportTask(caster, Destination));
        }
    }
}