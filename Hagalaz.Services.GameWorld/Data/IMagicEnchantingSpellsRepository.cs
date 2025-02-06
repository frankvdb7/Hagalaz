using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IMagicEnchantingSpellsRepository
    {
        public IQueryable<SkillsMagicEnchantDefinition> FindAll();
    }
}
