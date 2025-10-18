using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that retrieves scripts for equipment items.
    /// These scripts contain logic that executes when an item is equipped, such as applying stat bonuses or special effects.
    /// </summary>
    public interface IEquipmentScriptProvider
    {
        /// <summary>
        /// Finds and retrieves an equipment script by its associated item ID.
        /// </summary>
        /// <param name="itemId">The unique identifier of the equipment item.</param>
        /// <returns>The <see cref="IEquipmentScript"/> for the specified item, or a default/null script if not found.</returns>
        IEquipmentScript FindEquipmentScriptById(int itemId);
    }
}