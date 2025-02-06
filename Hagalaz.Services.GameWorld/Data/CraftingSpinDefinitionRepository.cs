using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class CraftingSpinDefinitionRepository : RepositoryBase<SkillsCraftingSpinDefinition>, ICraftingSpinDefinitionRepository
    {
        public CraftingSpinDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
