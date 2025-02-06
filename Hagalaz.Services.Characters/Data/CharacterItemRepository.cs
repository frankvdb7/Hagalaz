using System.Linq;
using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterItemRepository : RepositoryBase<CharactersItem>, ICharacterItemRepository
    {
        public CharacterItemRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersItem> FindByMasterId(uint masterId) => FindAll().Where(e => e.MasterId == masterId);
    }
}
