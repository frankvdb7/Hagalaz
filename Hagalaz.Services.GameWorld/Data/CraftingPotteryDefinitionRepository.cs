using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class CraftingPotteryDefinitionRepository : RepositoryBase<SkillsCraftingPotteryDefinition>, ICraftingPotteryDefinitionRepository
    {
        public CraftingPotteryDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
