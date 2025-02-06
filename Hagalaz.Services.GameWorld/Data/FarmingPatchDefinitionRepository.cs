using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class FarmingPatchDefinitionRepository : RepositoryBase<SkillsFarmingPatchDefinition>, IFarmingPatchDefinitionRepository
    {
        public FarmingPatchDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
