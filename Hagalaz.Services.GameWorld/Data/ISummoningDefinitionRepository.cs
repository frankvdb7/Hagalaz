using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ISummoningDefinitionRepository
    {
        public IQueryable<SkillsSummoningDefinition> FindAll();
    }
}
