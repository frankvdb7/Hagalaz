namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the target type for a Summoning familiar's special move.
    /// </summary>
    public enum FamiliarSpecialType
    {
        /// <summary>
        /// The special move is cast on an item.
        /// </summary>
        Item,
        /// <summary>
        /// The special move is cast on a creature.
        /// </summary>
        Creature,
        /// <summary>
        /// The special move is activated by a simple click, requiring no target.
        /// </summary>
        Click,
        /// <summary>
        /// The special move is cast on a game object.
        /// </summary>
        Object
    }
}
