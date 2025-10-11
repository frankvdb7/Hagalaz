using Hagalaz.Game.Abstractions.Collections;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Represents a data transfer object containing information about an item to be given to a newly created character.
    /// </summary>
    public class CharacterCreateInfoDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the item.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the quantity of the item.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the type of container where the item should be placed (e.g., inventory, bank).
        /// </summary>
        public ItemContainerType Type { get; set; }
    }
}