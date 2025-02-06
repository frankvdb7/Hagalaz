using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IMusicDefinitionRepository
    {
        public IQueryable<MusicDefinition> FindAll();
    }
}
