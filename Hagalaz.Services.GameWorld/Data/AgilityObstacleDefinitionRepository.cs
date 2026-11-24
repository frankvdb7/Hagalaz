using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class AgilityObstacleDefinitionRepository : RepositoryBase<SkillsAgilityObstacleDefinition>, IAgilityObstacleDefinitionRepository
    {
        public AgilityObstacleDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
