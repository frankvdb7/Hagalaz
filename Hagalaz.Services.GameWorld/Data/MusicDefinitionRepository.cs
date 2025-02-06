using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MusicDefinitionRepository : RepositoryBase<MusicDefinition>, IMusicDefinitionRepository
    {
        public MusicDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
