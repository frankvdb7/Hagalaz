using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class GroundItemSpawnRepository : RepositoryBase<ItemSpawn>, IGroundItemSpawnRepository
    {
        public GroundItemSpawnRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<ItemSpawn> FindByBounds(int minX, int minY, int maxX, int maxY) => FindAll().Where(s => s.CoordX >= minX && s.CoordX <= maxX && s.CoordY >= minY && s.CoordY <= maxY);
    }
}
