using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class CraftingJewelryDefinitionRepository : RepositoryBase<SkillsCraftingJewelryDefinition>, ICraftingJewelryDefinitionRepository
    {
        public CraftingJewelryDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
