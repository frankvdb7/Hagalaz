namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the stacking behavior for items within a container.
    /// </summary>
    public enum StorageType : byte
    {
        /// <summary>
        /// In this mode, all items are forced to stack, regardless of their individual stackable property. This is typical for containers like a player's bank.
        /// </summary>
        AlwaysStack,

        /// <summary>
        /// In this mode, items only stack if their definition marks them as stackable (e.g., coins, arrows) or if they are in a noted form. This is the standard behavior for an inventory.
        /// </summary>
        Normal
    }
}
