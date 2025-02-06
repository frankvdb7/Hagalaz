using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class NpcDefinitionRepository : RepositoryBase<NpcDefinition>, INpcDefinitionRepository
    {
        public NpcDefinitionRepository(HagalazDbContext dbContext) : base(dbContext) { }
    }
}