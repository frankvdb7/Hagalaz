using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class CraftingGemDefinitionRepository : RepositoryBase<SkillsCraftingGemDefinition>, ICraftingGemDefinitionRepository
    {
        public CraftingGemDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
