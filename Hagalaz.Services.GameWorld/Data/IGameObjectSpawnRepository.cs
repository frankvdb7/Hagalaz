using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IGameObjectSpawnRepository
    {
        public IQueryable<GameobjectSpawn> FindByBounds(int minX, int minY, int maxX, int maxY);
    }
}