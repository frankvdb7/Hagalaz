using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class MusicLocationRepository : RepositoryBase<MusicLocation>, IMusicLocationRepository
    {
        public MusicLocationRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
