using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class GameObjectLootRepository : RepositoryBase<GameobjectLoot>, IGameObjectLootRepository
    {
        public GameObjectLootRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
