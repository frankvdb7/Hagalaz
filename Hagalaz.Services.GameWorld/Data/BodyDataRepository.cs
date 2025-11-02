using System;
using System.Threading;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class BodyDataRepository : IBodyDataRepository
    {
        private readonly Lazy<IEquipmentDefaults> _equipmentDefaults;

        /// <summary>
        /// 
        /// </summary>
        public int BodySlotCount => _equipmentDefaults.Value.BodySlotData.Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyDataRepository"/> class.
        /// </summary>
        /// <param name="equipmentDefaultsProvider">The equipment defaults provider.</param>
        public BodyDataRepository(IEquipmentDefaultsProvider equipmentDefaultsProvider) => _equipmentDefaults = new Lazy<IEquipmentDefaults>(equipmentDefaultsProvider.Get, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Determines whether [is disabled slot] [the specified part].
        /// </summary>
        /// <param name="part">The part.</param>
        /// <returns>
        ///   <c>true</c> if [is disabled slot] [the specified part]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDisabledSlot(BodyPart part) => _equipmentDefaults.Value.BodySlotData[(byte)part] == 1;
    }
}