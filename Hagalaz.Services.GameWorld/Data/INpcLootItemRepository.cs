using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface INpcLootItemRepository
    {
        public IQueryable<NpcLootItem> FindAll();
    }
}
