using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that retrieves scripts for items.
    /// These scripts contain logic for item interactions, such as using, dropping, or examining an item.
    /// </summary>
    public interface IItemScriptProvider
    {
        /// <summary>
        /// Finds and retrieves an item script by its unique identifier.
        /// </summary>
        /// <param name="itemId">The unique identifier of the item.</param>
        /// <returns>The <see cref="IItemScript"/> for the specified item, or a default/null script if not found.</returns>
        IItemScript FindItemScriptById(int itemId);
    }
}