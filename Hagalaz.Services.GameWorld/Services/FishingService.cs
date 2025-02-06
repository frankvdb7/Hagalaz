using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    public class FishingService : IFishingService
    {
        // TODO fishingstore
        private readonly FishingStore _fishingStore;

        public FishingService(FishingStore fishingStore)
        {
            _fishingStore = fishingStore;
        }
        public Task<IFishingSpotTable?> FindSpotByNpcId(int npcId) => Task.FromResult<IFishingSpotTable?>(_fishingStore.FishingSpots.FirstOrDefault(spot => spot.NpcIds.Contains(npcId)));

        public Task<IFishingSpotTable?> FindSpotByNpcIdClickType(int npcId, NpcClickType clickType) => Task.FromResult<IFishingSpotTable?>(_fishingStore.FishingSpots.FirstOrDefault(spot => spot.ClickType == clickType && spot.NpcIds.Contains(npcId)));

        public Task<IReadOnlyList<IFishingSpotTable>> FindAllSpots() => Task.FromResult<IReadOnlyList<IFishingSpotTable>>(_fishingStore.FishingSpots);
    }
}