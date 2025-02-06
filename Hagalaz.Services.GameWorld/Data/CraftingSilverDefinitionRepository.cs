using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class CraftingSilverDefinitionRepository : RepositoryBase<SkillsCraftingSilverDefinition>, ICraftingSilverDefinitionRepository
    {
        public CraftingSilverDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
