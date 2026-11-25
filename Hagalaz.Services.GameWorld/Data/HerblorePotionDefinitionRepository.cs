using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class HerblorePotionDefinitionRepository : RepositoryBase<SkillsHerblorePotionDefinition>, IHerblorePotionDefinitionRepository
    {
        public HerblorePotionDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
