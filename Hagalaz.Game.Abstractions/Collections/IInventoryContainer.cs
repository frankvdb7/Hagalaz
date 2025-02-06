using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// 
    /// </summary>
    public interface IInventoryContainer : IItemContainer
    {
        /// <summary>
        /// Drop's specific item.
        /// </summary>
        /// <param name="item">Item which should be dropped.</param>
        /// <returns>If item was dropped successfully.</returns>
        bool DropItem(IItem item);
    }
}
