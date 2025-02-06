using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{

    public class HerbloreRepository : RepositoryBase<SkillsHerbloreHerbDefinition>, IHerbloreRepository
    {
        public HerbloreRepository(HagalazDbContext dbContext) : base(dbContext)
        {
        }

        public IQueryable<SkillsHerbloreHerbDefinition> FindGrimyHerbById(int herbId) => FindAll().Where(herb => herb.GrimyItemId == herbId);
    }
}