using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ISlayerMasterDefinitionRepository
    {
        public IQueryable<SkillsSlayerMasterDefinition> FindAll();
    }
}
