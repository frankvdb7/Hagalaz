using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class AgilityCourseDefinitionRepository : RepositoryBase<SkillsAgilityCourseDefinition>, IAgilityCourseDefinitionRepository
    {
        public AgilityCourseDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
