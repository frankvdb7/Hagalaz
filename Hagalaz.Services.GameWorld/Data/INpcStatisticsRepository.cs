using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface INpcStatisticsRepository
    {
        public IQueryable<NpcStatistic> FindAll();
    }
}