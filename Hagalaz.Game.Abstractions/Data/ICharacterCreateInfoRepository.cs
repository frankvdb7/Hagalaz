using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Data
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICharacterCreateInfoRepository
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerable<CharacterCreateInfoDto> FindAllContainerItems();
    }
}