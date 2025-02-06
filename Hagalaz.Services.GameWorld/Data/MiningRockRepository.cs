using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MiningRockRepository : RepositoryBase<SkillsMiningRockDefinition>, IMiningRockRepository
    {
        public MiningRockRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
