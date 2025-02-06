using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBankContainer : IItemContainer
    {
        /// <summary>
        /// Deposits from money pouch.
        /// </summary>
        /// <param name="deposited">The deposited.</param>
        /// <returns></returns>
        bool DepositFromMoneyPouch([NotNullWhen(true)]  out IItem? deposited);

        /// <summary>
        /// Deposit's specific item into character's bank.
        /// </summary>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <param name="deposited">Pointer to item which was deposited into bank. Can be null.</param>
        /// <param name="container"></param>
        /// <returns>If depositing was sucessfull.</returns>
        bool DepositFromFamiliar(IItem item, int count, [NotNullWhen(true)] out IItem? deposited, IItemContainer container);
        /// <summary>
        /// Deposit's specific item into character's bank.
        /// </summary>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <param name="deposited">Pointer to item which was deposited into bank. Can be null.</param>
        /// <returns>If depositing was sucessfull.</returns>
        bool DepositFromEquipment(IItem item, int count, [NotNullWhen(true)] out IItem? deposited);
        /// <summary>
        /// Deposit's specific item into character's bank.
        /// </summary>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <param name="deposited">Pointer to item which was deposited into bank. Can be null.</param>
        /// <returns>If depositing was sucessfull.</returns>
        bool DepositFromInventory(IItem item, int count, [NotNullWhen(true)] out IItem? deposited);
        /// <summary>
        /// Withdraw's specific item from character's bank and
        /// stores it into character's inventory.
        /// </summary>
        /// <param name="item">Item in character's bank which will be withdrawed.</param>
        /// <param name="count">Count of how much items should be withdrawed.</param>
        /// <param name="notingEnabled">Wheter noting is enabled in bank.</param>
        /// <param name="withdrawed">Pointer to item which will be deposited into character's inventory.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool WithdrawFromBank(IItem item, int count, bool notingEnabled, [NotNullWhen(true)] out IItem? withdrawed);
    }
}
