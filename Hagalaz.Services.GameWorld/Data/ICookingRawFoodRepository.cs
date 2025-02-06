using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ICookingRawFoodRepository
    {
        public IQueryable<SkillsCookingRawFoodDefinition> FindAll();
    }
}
