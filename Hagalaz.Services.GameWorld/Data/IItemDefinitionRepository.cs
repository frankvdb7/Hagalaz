using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IItemDefinitionRepository
    {
        public IQueryable<ItemDefinition> FindAll();
    }
}
