using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class LodestoneRepository : RepositoryBase<GameobjectLodestone>, ILodestoneRepository
    {
        public LodestoneRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
