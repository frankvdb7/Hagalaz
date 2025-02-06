using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class CookingRawFoodRepository : RepositoryBase<SkillsCookingRawFoodDefinition>, ICookingRawFoodRepository
    {
        public CookingRawFoodRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
