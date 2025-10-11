namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the various types of item containers available to a character.
    /// </summary>
    public enum ItemContainerType : sbyte
    {
        /// <summary>
        /// The player's bank, used for long-term storage.
        /// </summary>
        Bank = 0,

        /// <summary>
        /// The player's equipment, holding all currently worn items.
        /// </summary>
        Equipment = 1,

        /// <summary>
        /// The player's inventory, holding items the character is carrying.
        /// </summary>
        Inventory = 2,

        /// <summary>
        /// The inventory of a player's summoned familiar (e.g., a beast of burden).
        /// </summary>
        FamiliarInventory = 3,

        /// <summary>
        /// A temporary container for items earned from quests or activities that have not yet been claimed.
        /// </summary>
        Reward = 4,

        /// <summary>
        /// A special container that holds a player's coins separately from their main inventory.
        /// </summary>
        MoneyPouch = 5,
    }
}