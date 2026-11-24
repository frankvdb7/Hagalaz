using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class FletchingDefinitionRepository : RepositoryBase<SkillsFletchingDefinition>, IFletchingDefinitionRepository
    {
        public FletchingDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
