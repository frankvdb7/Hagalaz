using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRewardContainer : IItemContainer
    {
        /// <summary>
        /// Withdraws from reward container.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        int Claim(IItem item, int count);
    }
}
