namespace Hagalaz.Cache.Abstractions.Model
{
    /// <summary>
    /// Specifies the behavior of an item when it is lost, for example, upon a character's death.
    /// </summary>
    public enum DegradeType
    {
        /// <summary>
        /// The item is permanently destroyed and removed from the game.
        /// </summary>
        DestroyItem = -1,

        /// <summary>
        /// The item is dropped on the ground at the character's location, making it available for other players to pick up.
        /// </summary>
        DropItem = 0,

        /// <summary>
        /// The item is protected and remains in the character's inventory. This may be conditional,
        /// for example, when not in a PvP area like the wilderness.
        /// </summary>
        ProtectedItem = 1
    }
}
