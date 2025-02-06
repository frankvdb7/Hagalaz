using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class NpcLootItemRepository : RepositoryBase<NpcLootItem>, INpcLootItemRepository
    {
        public NpcLootItemRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
