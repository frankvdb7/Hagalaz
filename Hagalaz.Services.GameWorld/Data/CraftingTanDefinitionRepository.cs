using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class CraftingTanDefinitionRepository : RepositoryBase<SkillsCraftingTanDefinition>, ICraftingTanDefinitionRepository
    {
        public CraftingTanDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
