using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ICraftingSilverDefinitionRepository
    {
        public IQueryable<SkillsCraftingSilverDefinition> FindAll();
    }
}
