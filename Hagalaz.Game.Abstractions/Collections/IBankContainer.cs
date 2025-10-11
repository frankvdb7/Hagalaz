using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the contract for a player's bank container, extending the base <see cref="IItemContainer"/>
    /// with methods specific to bank operations like depositing from other containers and withdrawing.
    /// </summary>
    public interface IBankContainer : IItemContainer
    {
        /// <summary>
        /// Deposits the contents of the player's money pouch into the bank.
        /// </summary>
        /// <param name="deposited">When this method returns, contains the <see cref="IItem"/> representing the coins that were deposited, if the operation was successful.</param>
        /// <returns><c>true</c> if the deposit was successful; otherwise, <c>false</c>.</returns>
        bool DepositFromMoneyPouch([NotNullWhen(true)]  out IItem? deposited);

        /// <summary>
        /// Deposits a specific item from a familiar's inventory into the bank.
        /// </summary>
        /// <param name="item">The item to be deposited.</param>
        /// <param name="count">The number of items to deposit.</param>
        /// <param name="deposited">When this method returns, contains the <see cref="IItem"/> that was actually deposited into the bank, if the operation was successful.</param>
        /// <param name="container">The source familiar inventory container.</param>
        /// <returns><c>true</c> if the deposit was successful; otherwise, <c>false</c>.</returns>
        bool DepositFromFamiliar(IItem item, int count, [NotNullWhen(true)] out IItem? deposited, IItemContainer container);

        /// <summary>
        /// Deposits a specific item from the player's equipment into the bank.
        /// </summary>
        /// <param name="item">The item to be deposited.</param>
        /// <param name="count">The number of items to deposit.</param>
        /// <param name="deposited">When this method returns, contains the <see cref="IItem"/> that was actually deposited into the bank, if the operation was successful.</param>
        /// <returns><c>true</c> if the deposit was successful; otherwise, <c>false</c>.</returns>
        bool DepositFromEquipment(IItem item, int count, [NotNullWhen(true)] out IItem? deposited);

        /// <summary>
        /// Deposits a specific item from the player's inventory into the bank.
        /// </summary>
        /// <param name="item">The item to be deposited.</param>
        /// <param name="count">The number of items to deposit.</param>
        /// <param name="deposited">When this method returns, contains the <see cref="IItem"/> that was actually deposited into the bank, if the operation was successful.</param>
        /// <returns><c>true</c> if the deposit was successful; otherwise, <c>false</c>.</returns>
        bool DepositFromInventory(IItem item, int count, [NotNullWhen(true)] out IItem? deposited);

        /// <summary>
        /// Withdraws a specific item from the bank and prepares it for placement in another container (e.g., inventory).
        /// </summary>
        /// <param name="item">The item in the bank to be withdrawn.</param>
        /// <param name="count">The number of items to withdraw.</param>
        /// <param name="notingEnabled">A flag indicating whether the item should be withdrawn in its noted form if possible.</param>
        /// <param name="withdrawed">When this method returns, contains the <see cref="IItem"/> instance that was withdrawn from the bank, if the operation was successful.</param>
        /// <returns><c>true</c> if the withdrawal was successful; otherwise, <c>false</c>.</returns>
        bool WithdrawFromBank(IItem item, int count, bool notingEnabled, [NotNullWhen(true)] out IItem? withdrawed);
    }
}
