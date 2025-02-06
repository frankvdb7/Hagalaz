using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class SlayerMasterDefinitionRepository : RepositoryBase<SkillsSlayerMasterDefinition>, ISlayerMasterDefinitionRepository
    {
        public SlayerMasterDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
