using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IMusicLocationRepository
    {
        public IQueryable<MusicLocation> FindAll();
    }
}
