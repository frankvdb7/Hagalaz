using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface INpcSpawnRepository
    {
        IQueryable<NpcSpawn> FindByBounds(int minX, int minY, int maxX, int maxY);
    }
}