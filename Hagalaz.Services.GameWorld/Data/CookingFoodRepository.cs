using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class CookingFoodRepository : RepositoryBase<SkillsCookingFoodDefinition>, ICookingFoodRepository
    {
        public CookingFoodRepository(HagalazDbContext dbContext) : base(dbContext) { }
    }
}
