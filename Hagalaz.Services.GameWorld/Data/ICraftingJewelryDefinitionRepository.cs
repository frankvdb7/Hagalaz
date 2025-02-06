using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ICraftingJewelryDefinitionRepository
    {
        public IQueryable<SkillsCraftingJewelryDefinition> FindAll();
    }
}
