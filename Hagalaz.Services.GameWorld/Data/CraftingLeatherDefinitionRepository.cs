using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class CraftingLeatherDefinitionRepository : RepositoryBase<SkillsCraftingLeatherDefinition>, ICraftingLeatherDefinitionRepository
    {
        public CraftingLeatherDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
