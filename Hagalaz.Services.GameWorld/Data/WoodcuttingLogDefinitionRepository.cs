using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class WoodcuttingLogDefinitionRepository : RepositoryBase<SkillsWoodcuttingLogDefinition>, IWoodcuttingLogDefinitionRepository
    {
        public WoodcuttingLogDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
