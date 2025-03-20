using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Fishing
{
    public class FishingSpotScriptFactory : INpcScriptFactory
    {
        private readonly IFishingService _fishingService;

        public FishingSpotScriptFactory(IFishingService fishingService)
        {
            _fishingService = fishingService;
        }

        public async IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var scriptType = typeof(FishingSpot);
            var spots = await _fishingService.FindAllSpots(cancellationToken);
            foreach (var id in spots.SelectMany(s => s.NpcIds).Distinct())
            {
                yield return (id, scriptType);
            }
        }
    }
}
