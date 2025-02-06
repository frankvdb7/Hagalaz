using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Class for managing equipment related things.
    /// </summary>
    public interface IEquipmentService
    {
        /// <summary>
        /// Gets the equipment definition.
        /// </summary>
        /// <param name="itemID">The item identifier.</param>
        /// <returns></returns>
        IEquipmentDefinition FindEquipmentDefinitionById(int itemID);
    }
}