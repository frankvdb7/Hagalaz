namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMoneyPouchContainer : IItemContainer
    {
        /// <summary>
        /// Gets the examine.
        /// </summary>
        /// <value>
        /// The examine.
        /// </value>
        string Examine { get; }
        /// <summary>
        /// Contains the money count.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Adds the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        bool Add(int count);
        /// <summary>
        /// Adds from inventory.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        bool AddFromInventory(int count);
        /// <summary>
        /// Moves to inventory.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        bool MoveToInventory(int count);
        /// <summary>
        /// Removes the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        int Remove(int count);
    }
}
