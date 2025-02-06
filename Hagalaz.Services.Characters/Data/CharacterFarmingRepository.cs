using System.Linq;
using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterFarmingRepository : RepositoryBase<CharactersFarmingPatch>, ICharacterFarmingRepository
    {
        public CharacterFarmingRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersFarmingPatch> FindById(uint masterId) => FindAll().Where(e => e.MasterId == masterId);
    }
}
