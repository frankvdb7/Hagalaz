using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IRunecraftingRepository
    {
        public IQueryable<SkillsRunecraftingDefinition> FindAll();
    }
}
