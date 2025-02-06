using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IWoodcuttingTreeDefinitionRepository
    {
        public IQueryable<SkillsWoodcuttingTreeDefinition> FindAll();
    }
}
