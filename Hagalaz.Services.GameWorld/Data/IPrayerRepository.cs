using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IPrayerRepository
    {
        public IQueryable<SkillsPrayerDefinition> FindAll();
    }
}
