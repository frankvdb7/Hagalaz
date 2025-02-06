using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class RunecraftingRepository : RepositoryBase<SkillsRunecraftingDefinition>, IRunecraftingRepository
    {
        public RunecraftingRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
