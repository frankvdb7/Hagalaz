using Hagalaz.Data.Entities;
using Hagalaz.Data;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class GameObjectDefinitionRepository : RepositoryBase<GameobjectDefinition>, IGameObjectDefinitionRepository
    {
        public GameObjectDefinitionRepository(HagalazDbContext dbContext)
            : base(dbContext)
        {
        }
    }
}
