using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ICraftingSpinDefinitionRepository
    {
        public IQueryable<SkillsCraftingSpinDefinition> FindAll();
    }
}
