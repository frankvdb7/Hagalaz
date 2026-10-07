using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the contract for a player's money pouch, a special container that holds coins separately from the main inventory.
    /// </summary>
    public interface IMoneyPouchContainer : IItemTransactional
    {
        bool HasSpaceForCoins(int count);

        bool HasCoins(int count);

        /// <summary>
        /// Gets the "Examine" text for the money pouch, which typically displays the total number of coins.
        /// </summary>
        string Examine { get; }

        /// <summary>
        /// Gets the total number of coins in the money pouch.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds exactly the requested number of coins, using the inventory for any amount that overflows the pouch.
        /// The addition is all-or-nothing.
        /// </summary>
        /// <param name="count">The number of coins to add.</param>
        /// <returns><c>true</c> if the coins were added successfully; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// When none of the pouch's required storage is currently enlisted, this operation creates and owns one
        /// <see cref="ItemContainerTransaction"/> covering the pouch and inventory storage. When every required storage
        /// already belongs to the same active current-thread transaction, this operation participates in that
        /// caller-owned transaction and does not commit it. When only part of the required storage is enlisted, or
        /// required storage belongs to different transactions, this operation throws
        /// <see cref="InvalidOperationException"/> before mutation.
        /// </remarks>
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
        /// Transfers the full coin stack represented by <paramref name="coins"/> from an already-enlisted item
        /// container into this pouch.
        /// </summary>
        /// <remarks>
        /// This operation requires the source, pouch, and inventory storage to belong to the same active
        /// caller-owned <see cref="ItemContainerTransaction"/>. It never creates a transaction.
        /// </remarks>
        bool TryTransferCoinsFrom(IItemContainer source, IItem coins, int preferredSourceSlot = -1);

        /// <summary>
        /// Removes exactly the requested number of coins from the pouch and inventory, if available.
        /// </summary>
        /// <remarks>
        /// When none of the pouch's required storage is currently enlisted, this operation creates and owns one
        /// <see cref="ItemContainerTransaction"/> covering the pouch and inventory storage. When every required storage
        /// already belongs to the same active current-thread transaction, this operation participates in that
        /// caller-owned transaction and does not commit it. When only part of the required storage is enlisted, or
        /// required storage belongs to different transactions, this operation throws
        /// <see cref="InvalidOperationException"/> before mutation.
        /// </remarks>
        bool TryRemoveExact(int count);
    }
}
