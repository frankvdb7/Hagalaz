using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class ItemDefinitionRepository : RepositoryBase<ItemDefinition>, IItemDefinitionRepository
    {
        public ItemDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
