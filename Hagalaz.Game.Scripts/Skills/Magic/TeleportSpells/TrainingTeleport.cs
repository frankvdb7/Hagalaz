using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells
{
    /// <summary>
    ///     Contains training teleport script.
    /// </summary>
    public class TrainingTeleport : TeleportSpellScript
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TrainingTeleport" /> class.
        /// </summary>
        public TrainingTeleport()
        {
            Book = MagicBook.StandardBook;
            Destination = Location.Create(2885, 9830, 0, 0);
        }
    }
}