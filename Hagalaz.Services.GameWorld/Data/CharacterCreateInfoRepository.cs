using System.Collections.Generic;
using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class CharacterCreateInfoRepository : RepositoryBase<CharacterscreateinfoItem>, ICharacterCreateInfoRepository
    {
        public CharacterCreateInfoRepository(HagalazDbContext context)
            : base(context)
        {

        }

        public IEnumerable<CharacterCreateInfoDto> FindAllContainerItems() => FindAll().Select(item => new CharacterCreateInfoDto
        {
            Id = item.ItemId,
            Count = item.Count,
            Type = (ItemContainerType)item.ContainerType
        });
    }
}