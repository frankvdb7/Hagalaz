using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class FarmingSeedDefinitionRepository : RepositoryBase<SkillsFarmingSeedDefinition>, IFarmingSeedDefinitionRepository
    {
        public FarmingSeedDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
