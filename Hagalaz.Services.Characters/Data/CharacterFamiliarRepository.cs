using System.Linq;
using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterFamiliarRepository : RepositoryBase<CharactersFamiliar>, ICharacterFamiliarRepository
    {
        public CharacterFamiliarRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersFamiliar> FindById(uint masterId) => FindAll().Where(c => c.MasterId == masterId);
    }
}
