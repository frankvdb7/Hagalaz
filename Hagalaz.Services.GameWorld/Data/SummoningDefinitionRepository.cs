using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class SummoningDefinitionRepository : RepositoryBase<SkillsSummoningDefinition>, ISummoningDefinitionRepository
    {
        public SummoningDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
