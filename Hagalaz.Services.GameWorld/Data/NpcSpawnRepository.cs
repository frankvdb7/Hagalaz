using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class NpcSpawnRepository : RepositoryBase<NpcSpawn>, INpcSpawnRepository
    {
        public NpcSpawnRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<NpcSpawn> FindByBounds(int minX, int minY, int maxX, int maxY) => FindAll().Where(s => s.CoordX >= minX && s.CoordX <= maxX && s.CoordY >= minY && s.CoordY <= maxY);
    }
}
