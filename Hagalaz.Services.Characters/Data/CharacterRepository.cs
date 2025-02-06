using System.Linq;
using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterRepository : RepositoryBase<Character>, ICharacterRepository
    {
        public CharacterRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<Character> FindById(uint masterId) => FindAll().Where(e => e.Id == masterId);
    }
}
