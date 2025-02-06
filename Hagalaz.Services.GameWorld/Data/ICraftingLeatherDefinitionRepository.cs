using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ICraftingLeatherDefinitionRepository
    {
        public IQueryable<SkillsCraftingLeatherDefinition> FindAll();
    }
}
