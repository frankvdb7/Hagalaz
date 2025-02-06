using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class SlayerTaskDefinitionRepository : RepositoryBase<SkillsSlayerTaskDefinition>, ISlayerTaskDefinitionRepository
    {
        public SlayerTaskDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
