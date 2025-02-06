using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameWorld.Data
{

    public interface IHerbloreRepository
    {
        public IQueryable<SkillsHerbloreHerbDefinition> FindGrimyHerbById(int herbId);
        public IQueryable<SkillsHerbloreHerbDefinition> FindAll();
    }
}