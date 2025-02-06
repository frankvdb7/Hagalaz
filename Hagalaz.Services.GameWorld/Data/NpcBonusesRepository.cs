using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class NpcBonusesRepository : RepositoryBase<NpcBonuses>, INpcBonusesRepository
    {
        public NpcBonusesRepository(HagalazDbContext context) : base(context) { }
    }
}