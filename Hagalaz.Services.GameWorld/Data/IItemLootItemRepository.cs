using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IItemLootItemRepository
    {
        public IQueryable<ItemLootItem> FindAll();
    }
}
