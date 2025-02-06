using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IMagicCombatSpellsRepository
    {
        public IQueryable<SkillsMagicCombatDefinition> FindAll();
    }
}
