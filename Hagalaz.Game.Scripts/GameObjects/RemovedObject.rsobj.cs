using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.GameObjects
{
    [GameObjectScriptMetaData([1596, 1597, 31825, 31827, 31841, 31844, 38453, 38447, 55349, 77085, 77086, 77098])]
    public class RemovedObject : GameObjectScript
    {
        private readonly IMapRegionService _mapRegionService;

        public RemovedObject(IMapRegionService mapRegionService) => _mapRegionService = mapRegionService;

        /// <summary>
        ///     Happens when object is spawned.
        /// </summary>
        public override void OnSpawn() => _mapRegionService
            .GetOrCreateMapRegion(Owner.Location.RegionId, Owner.Location.Dimension, false)
            .Remove(Owner);

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() { }
    }
}
