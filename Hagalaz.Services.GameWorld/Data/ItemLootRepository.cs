using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class ItemLootRepository : RepositoryBase<ItemLoot>, IItemLootRepository
    {
        public ItemLootRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
