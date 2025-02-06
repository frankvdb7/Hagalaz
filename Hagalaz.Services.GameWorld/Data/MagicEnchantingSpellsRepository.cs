using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MagicEnchantingSpellsRepository : RepositoryBase<SkillsMagicEnchantDefinition>, IMagicEnchantingSpellsRepository
    {
        public MagicEnchantingSpellsRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
