using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class NpcLootRepository : RepositoryBase<NpcLoot>, INpcLootRepository
    {
        public NpcLootRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
