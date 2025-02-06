using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IMiningOreRepository
    {
        public IQueryable<SkillsMiningOreDefinition> FindAll();
    }
}
