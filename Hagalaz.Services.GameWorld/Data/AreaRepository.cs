using Hagalaz.Data;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class AreaRepository : RepositoryBase<Hagalaz.Data.Entities.Area>, IAreaRepository
    {
        public AreaRepository(HagalazDbContext dbContext)
            : base(dbContext)
        {
        }
    }
}