using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IFishingSpotDefinitionRepository
    {
        public IQueryable<SkillsFishingSpotDefinition> FindAll();
    }
}
