using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MagicEnchantingSpellsProductRepository : RepositoryBase<SkillsMagicEnchantProduct>, IMagicEnchantingSpellsProductRepository
    {
        public MagicEnchantingSpellsProductRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
