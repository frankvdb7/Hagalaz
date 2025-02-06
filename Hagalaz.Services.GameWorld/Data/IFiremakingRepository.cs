using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IFiremakingRepository
    {
        public IQueryable<SkillsFiremakingDefinition> FindAll();
    }
}
