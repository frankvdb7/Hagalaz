using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IItemService
    {
        /// <summary>
        /// Gets the item definition.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns></returns>
        IItemDefinition FindItemDefinitionById(int itemId);
        /// <summary>
        /// Gets the total item count
        /// </summary>
        /// <returns></returns>
        int GetTotalItemCount();
    }
}