using System.Linq;
using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterLookRepository : RepositoryBase<CharactersLook>, ICharacterLookRepository
    {
        public CharacterLookRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersLook> FindById(uint masterId) => FindAll().Where(e => e.MasterId == masterId);
    }
}
