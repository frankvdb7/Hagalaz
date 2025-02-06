using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IMagicEnchantingSpellsProductRepository
    {
        public IQueryable<SkillsMagicEnchantProduct> FindAll();
    }
}
