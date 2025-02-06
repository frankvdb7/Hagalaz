using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class ItemLootItemRepository : RepositoryBase<ItemLootItem>, IItemLootItemRepository
    {
        public ItemLootItemRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
