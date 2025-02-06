using Hagalaz.Game.Abstractions.Collections;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Class which contains data about an item.
    /// </summary>
    public class CharacterCreateInfoDto
    {
        /// <summary>
        /// The item Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The item count.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// The container type.
        /// </summary>
        public ItemContainerType Type { get; set; }
    }
}