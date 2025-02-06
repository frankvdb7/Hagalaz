using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class GameObjectLootItemRepository : RepositoryBase<GameobjectLootItem>, IGameObjectLootItemRepository
    {
        public GameObjectLootItemRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
