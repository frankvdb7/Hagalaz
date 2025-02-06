using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public class Farming : IFarming, IHydratable<HydratedFarmingDto>, IDehydratable<HydratedFarmingDto>
    {
        /// <summary>
        /// The owner.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// The patches.
        /// </summary>
        private readonly Dictionary<int, FarmingPatchTickTask> _patches = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Farming"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public Farming(ICharacter owner) => _owner = owner;

        /// <summary>
        /// Gets the or create farming patch.
        /// </summary>
        /// <param name="patchObjectID">The patch object identifier.</param>
        /// <returns></returns>
        public IFarmingPatch GetOrCreateFarmingPatch(int patchObjectID)
        {
            var patch = GetFarmingPatch(patchObjectID);
            if (patch != null)
            {
                return patch;
            }

            var patchTickTask = new FarmingPatchTickTask(_owner, patchObjectID);
            _patches.Add(patchObjectID, patchTickTask);
            _owner.QueueTask(patchTickTask);
            return patchTickTask;
        }

        /// <summary>
        /// Removes the farming patch.
        /// </summary>
        /// <param name="patchObjectID">The patch object identifier.</param>
        public void RemoveFarmingPatch(int patchObjectID)
        {
            //this.owner.Configurations.SendBITConfiguration(World.ObjectsManager.FindGameObjectDefinition(patch.PatchDefinition.ObjectID).VarpBitFileID, 0); // reset
            if (_patches.TryGetValue(patchObjectID, out var patch))
            {
                patch.Cancel();
                _patches.Remove(patchObjectID);
            }
        }

        /// <summary>
        /// Gets the farming patch.
        /// </summary>
        /// <param name="patchObjectID">The patch object identifier.</param>
        /// <returns></returns>
        public IFarmingPatch? GetFarmingPatch(int patchObjectID)
        {
            if (_patches.TryGetValue(patchObjectID, out var value))
                return value;
            return null;
        }

        public void Hydrate(HydratedFarmingDto hydration)
        {
            foreach (var hydratedPatch in hydration.Patches)
            {
                var patch = new FarmingPatchTickTask(_owner, hydratedPatch.Id);
                patch.Hydrate(hydratedPatch);
                _patches.Add(hydratedPatch.Id, patch);
                _owner.QueueTask(patch);
            }
        }
        public HydratedFarmingDto Dehydrate() => new()
        {
            Patches = _patches.Values.Select(patch => patch.Dehydrate()).ToList()
        };
    }
}