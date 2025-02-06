using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameWorld.Data
{
    public class TzhaarWaveDefinitionRepository : RepositoryBase<MinigamesTzhaarCaveWave>, ITzhaarWaveDefinitionRepository
    {
        public TzhaarWaveDefinitionRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
