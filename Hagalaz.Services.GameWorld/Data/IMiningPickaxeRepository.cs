using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IMiningPickaxeRepository
    {
        public IQueryable<SkillsMiningPickaxeDefinition> FindAll();
    }
}
