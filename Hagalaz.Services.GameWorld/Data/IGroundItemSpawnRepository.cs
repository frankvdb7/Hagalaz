using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IGroundItemSpawnRepository
    {
        public IQueryable<ItemSpawn> FindByBounds(int minX, int minY, int maxX, int maxY);
    }
}