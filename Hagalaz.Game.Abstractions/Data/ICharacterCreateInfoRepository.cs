using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Data
{
    /// <summary>
    /// Defines a contract for a repository that provides data for new character creation,
    /// such as the default items a character starts with.
    /// </summary>
    public interface ICharacterCreateInfoRepository
    {
        /// <summary>
        /// Retrieves a collection of all items that a new character should receive upon creation.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CharacterCreateInfoDto"/>, each representing an item for the new character.</returns>
        IEnumerable<CharacterCreateInfoDto> FindAllContainerItems();
    }
}