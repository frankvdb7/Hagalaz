using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class PrayerRepository : RepositoryBase<SkillsPrayerDefinition>, IPrayerRepository
    {
        public PrayerRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
