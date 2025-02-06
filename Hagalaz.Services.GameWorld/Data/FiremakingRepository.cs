using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class FiremakingRepository : RepositoryBase<SkillsFiremakingDefinition>, IFiremakingRepository
    {
        public FiremakingRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
