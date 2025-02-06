using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class EquipmentDefinitionRepository : RepositoryBase<EquipmentDefinition>, IEquipmentDefinitionRepository
    {
        public EquipmentDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
