using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class WoodcuttingTreeDefinitionRepository : RepositoryBase<SkillsWoodcuttingTreeDefinition>, IWoodcuttingTreeDefinitionRepository
    {
        public WoodcuttingTreeDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
