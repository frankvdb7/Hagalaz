using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MiningPickaxeRepository : RepositoryBase<SkillsMiningPickaxeDefinition>, IMiningPickaxeRepository
    {
        public MiningPickaxeRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
