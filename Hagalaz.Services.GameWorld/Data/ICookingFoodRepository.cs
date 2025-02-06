using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ICookingFoodRepository
    {
        public IQueryable<SkillsCookingFoodDefinition> FindAll();
    }
}
