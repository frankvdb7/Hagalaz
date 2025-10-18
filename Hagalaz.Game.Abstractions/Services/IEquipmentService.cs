using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages equipment definitions.
    /// </summary>
    public interface IEquipmentService
    {
        /// <summary>
        /// Finds an equipment definition by its item ID.
        /// </summary>
        /// <param name="itemID">The ID of the equippable item.</param>
        /// <returns>The <see cref="IEquipmentDefinition"/> for the item.</returns>
        IEquipmentDefinition FindEquipmentDefinitionById(int itemID);
    }
}