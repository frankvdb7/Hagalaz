using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells
{
    /// <summary>
    ///     Contains script for standard teleports.
    /// </summary>
    public class StandardTeleportScript : TeleportSpellScript
    {
        /// <summary>
        ///     The required level to cast this teleport.
        /// </summary>
        private readonly int _requiredLevel;

        /// <summary>
        ///     The required runes of this teleport.
        /// </summary>
        private readonly RuneType[] _runes;

        /// <summary>
        ///     The required rune amounts of this teleport.
        /// </summary>
        private readonly int[] _runeAmount;

        /// <summary>
        ///     Initializes a new instance of the <see cref="StandardTeleportScript" /> class.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="teleportDistance">The teleport distance.</param>
        public StandardTeleportScript(MagicBook book, int x, int y, int z, int teleportDistance)
        {
            Book = book;
            Destination = Location.Create(x, y, z, 0);
            TeleportDistance = teleportDistance;
            _runes = [];
            _runeAmount = [];
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="StandardTeleportScript" /> class.
        /// </summary>
        /// <param name="book">The book.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="teleportDistance">The teleport distance.</param>
        /// <param name="requiredLevel">The required level.</param>
        /// <param name="experience">The experience.</param>
        /// <param name="runes">The runes.</param>
        /// <param name="runeAmount">The rune amount.</param>
        public StandardTeleportScript(
            MagicBook book, int x, int y, int z, int teleportDistance, int requiredLevel, double experience, RuneType[] runes, int[] runeAmount)
        {
            Book = book;
            Destination = Location.Create(x, y, z, 0);
            TeleportDistance = teleportDistance;
            _requiredLevel = requiredLevel;
            MagicExperience = experience;
            _runes = runes;
            _runeAmount = runeAmount;
        }

        /// <summary>
        ///     Checks the requirements.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public override bool CanTeleport(ICharacter caster) => caster.Magic.CheckMagicLevel(_requiredLevel) && caster.Magic.CheckRunes(_runes, _runeAmount);

        /// <summary>
        ///     Gets the teleport distance.
        /// </summary>
        /// <value></value>
        public override int TeleportDistance { get; }

        /// <summary>
        ///     Gets the magic experience.
        /// </summary>
        /// <value></value>
        public override double MagicExperience { get; }

        /// <summary>
        ///     Removes the requirements.
        /// </summary>
        /// <param name="caster"></param>
        public override void OnTeleportStarted(ICharacter caster) => caster.Magic.RemoveRunes(_runes, _runeAmount);
    }
}