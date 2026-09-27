namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the contract for a player's money pouch, a special container that holds coins separately from the main inventory.
    /// </summary>
    public interface IMoneyPouchContainer : IItemContainer
    {
        object MutationLock { get; }

        long MutationOrder { get; }

        /// <summary>
        /// Gets the "Examine" text for the money pouch, which typically displays the total number of coins.
        /// </summary>
        string Examine { get; }

        /// <summary>
        /// Gets the total number of coins in the money pouch.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds a specified number of coins to the money pouch.
        /// </summary>
        /// <param name="count">The number of coins to add.</param>
        /// <returns><c>true</c> if the coins were added successfully; otherwise, <c>false</c>.</returns>
        bool Add(int count);

        /// <summary>
        /// Transfers a specified number of coins from the player's inventory to the money pouch.
        /// </summary>
        /// <param name="count">The number of coins to transfer.</param>
        /// <returns><c>true</c> if the transfer was successful; otherwise, <c>false</c>.</returns>
        bool AddFromInventory(int count);

        /// <summary>
        /// Moves a specified number of coins from the money pouch to the player's inventory.
        /// </summary>
        /// <param name="count">The number of coins to move.</param>
        /// <returns><c>true</c> if the coins were moved successfully; otherwise, <c>false</c>.</returns>
        bool MoveToInventory(int count);

        /// <summary>
        /// Removes a specified number of coins from the money pouch.
        /// </summary>
        /// <param name="count">The number of coins to remove.</param>
        /// <returns>The number of coins that were actually removed.</returns>
        int Remove(int count);

        /// <summary>
        /// Adds coins using the pouch's normal overflow rules as one checked trade operation.
        /// </summary>
        bool AddForTrade(int count);

        /// <summary>
        /// Removes coins using the pouch's normal underflow rules as one checked trade operation.
        /// </summary>
        bool RemoveForTrade(int count);

        /// <summary>
        /// Adds coins to the pouch and, if needed, Inventory without publishing changes.
        /// </summary>
        bool TryAddForTradeStorage(int count, out int pouchChangeCount,
            out System.Collections.Generic.HashSet<int> inventoryChangedSlots);

        /// <summary>
        /// Removes coins from the pouch and, if needed, Inventory without publishing changes.
        /// </summary>
        bool TryRemoveForTradeStorage(int count, out int pouchChangeCount,
            out System.Collections.Generic.HashSet<int> inventoryChangedSlots);

        /// <summary>
        /// Publishes a completed checked pouch change and its player message.
        /// </summary>
        void PublishTradeChanges(int pouchChangeCount);

        /// <summary>
        /// Removes exactly the requested number of coins from the pouch and inventory, if available.
        /// </summary>
        bool TryRemoveExact(int count);
    }
}
