using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages item definitions.
    /// </summary>
    public interface IItemService
    {
        /// <summary>
        /// Finds an item definition by its ID.
        /// </summary>
        /// <param name="itemId">The ID of the item definition to find.</param>
        /// <returns>The <see cref="IItemDefinition"/> for the item.</returns>
        IItemDefinition FindItemDefinitionById(int itemId);

        /// <summary>
        /// Gets the total number of unique item definitions loaded.
        /// </summary>
        /// <returns>The total count of item definitions.</returns>
        int GetTotalItemCount();
    }
}