using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class WoodcuttingHatchetDefinitionRepository : RepositoryBase<SkillsWoodcuttingHatchetDefinition>, IWoodcuttingHatchetDefinitionRepository
    {
        public WoodcuttingHatchetDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
