using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IWoodcuttingLogDefinitionRepository
    {
        public IQueryable<SkillsWoodcuttingLogDefinition> FindAll();
    }
}
