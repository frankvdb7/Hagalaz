using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ICraftingTanDefinitionRepository
    {
        public IQueryable<SkillsCraftingTanDefinition> FindAll();
    }
}
