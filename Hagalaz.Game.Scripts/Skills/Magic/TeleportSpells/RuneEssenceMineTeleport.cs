using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells
{
    /// <summary>
    ///     TODO - new anims...
    /// </summary>
    public class RuneEssenceMineTeleport : TeleportSpellScript
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RuneEssenceMineTeleport" /> class.
        /// </summary>
        public RuneEssenceMineTeleport()
        {
            Book = MagicBook.StandardBook;
            Destination = Location.Create(2910, 4832, 0, 0);
        }

        /// <summary>
        ///     Gets the teleport distance.
        /// </summary>
        /// <value></value>
        public override int TeleportDistance => 2;
    }
}