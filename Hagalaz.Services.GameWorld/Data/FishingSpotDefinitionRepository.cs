using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class FishingSpotDefinitionRepository : RepositoryBase<SkillsFishingSpotDefinition>, IFishingSpotDefinitionRepository
    {
        public FishingSpotDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
