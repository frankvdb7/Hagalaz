using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IEquipmentDefinitionRepository
    {
        public IQueryable<EquipmentDefinition> FindAll();
    }
}
