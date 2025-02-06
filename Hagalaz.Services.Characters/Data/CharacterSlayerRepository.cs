using System.Linq;
using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterSlayerRepository : RepositoryBase<CharactersSlayerTask>, ICharacterSlayerRepository
    {
        public CharacterSlayerRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersSlayerTask> FindById(uint masterId) => FindAll().Where(e => e.MasterId == masterId);
    }
}
