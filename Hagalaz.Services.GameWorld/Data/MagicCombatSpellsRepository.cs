using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MagicCombatSpellsRepository : RepositoryBase<SkillsMagicCombatDefinition>, IMagicCombatSpellsRepository
    {
        public MagicCombatSpellsRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
