using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ICraftingPotteryDefinitionRepository
    {
        public IQueryable<SkillsCraftingPotteryDefinition> FindAll();
    }
}
