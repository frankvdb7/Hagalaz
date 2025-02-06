using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class GameObjectSpawnRepository : RepositoryBase<GameobjectSpawn>, IGameObjectSpawnRepository
    {
        public GameObjectSpawnRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<GameobjectSpawn> FindByBounds(int minX, int minY, int maxX, int maxY) => FindAll().Where(s => s.CoordX >= minX && s.CoordX <= maxX && s.CoordY >= minY && s.CoordY <= maxY);
    }
}