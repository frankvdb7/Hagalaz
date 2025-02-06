using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells
{
    /// <summary>
    ///     Contains godwars teleport script.
    /// </summary>
    public class GodwarsTeleport : TeleportSpellScript
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GodwarsTeleport" /> class.
        /// </summary>
        public GodwarsTeleport()
        {
            Book = MagicBook.StandardBook;
            Destination = Location.Create(2915, 3734, 0, 0);
        }

        /// <summary>
        ///     Gets the teleport distance.
        /// </summary>
        /// <value></value>
        public override int TeleportDistance => 3;
    }
}