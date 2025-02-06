using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class NpcStatisticsRepository : RepositoryBase<NpcStatistic>, INpcStatisticsRepository
    {
        public NpcStatisticsRepository(HagalazDbContext context) : base(context)
        {

        }
    }
}