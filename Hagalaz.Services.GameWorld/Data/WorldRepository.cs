using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class WorldRepository : RepositoryBase<World>, IWorldRepository
    {
        public WorldRepository(HagalazDbContext context) : base(context) { }
        
        public IQueryable<World> FindWorldById(int worldId) => FindAll().Where(world => world.Id == worldId);
    }
}