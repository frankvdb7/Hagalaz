using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IMiningRockRepository
    {
        public IQueryable<SkillsMiningRockDefinition> FindAll();
    }
}
