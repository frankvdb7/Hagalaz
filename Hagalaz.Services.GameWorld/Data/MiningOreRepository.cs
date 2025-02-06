using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MiningOreRepository : RepositoryBase<SkillsMiningOreDefinition>, IMiningOreRepository
    {
        public MiningOreRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
