using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameLogon.Data
{
    public class CharacterPreferencesRepository : RepositoryBase<CharactersPreference>, ICharacterPreferencesRepository
    {
        public CharacterPreferencesRepository(HagalazDbContext context) : base(context)
        {

        }

        public IQueryable<CharactersPreference> FindById(uint masterId) => FindAll().Where(c => c.MasterId == masterId);
    }
}