using System.Linq;
using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterItemLookRepository : RepositoryBase<CharactersItemsLook>, ICharacterItemLookRepository
    {
        public CharacterItemLookRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersItemsLook> FindById(uint masterId) => FindAll().Where(e => e.MasterId == masterId);
    }
}
