using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ITzhaarWaveDefinitionRepository
    {
        public IQueryable<MinigamesTzhaarCaveWave> FindAll();
    }
}
