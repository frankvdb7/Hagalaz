namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the different spellbooks a character can use, which determine the set of available spells.
    /// </summary>
    public enum MagicBook : int
    {
        /// <summary>
        /// The standard spellbook, available to all players.
        /// </summary>
        StandardBook = 192,
        /// <summary>
        /// The Ancient Magicks spellbook, focused on combat spells.
        /// </summary>
        AncientBook = 193,
        /// <summary>
        /// The Lunar spellbook, focused on utility and skilling spells.
        /// </summary>
        LunarBook = 430,
        /// <summary>
        /// The Dungeoneering spellbook, used within the Dungeoneering skill.
        /// </summary>
        DungeoneeringBook = 950,
    }
}
