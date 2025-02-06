using System;
using System.Threading;
using Hagalaz.Cache;
using Hagalaz.Cache.Types.Defaults;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class BodyDataRepository : IBodyDataRepository
    {
        private readonly Lazy<EquipmentDefaults> _equipmentDefaults;

        /// <summary>
        /// 
        /// </summary>
        public int BodySlotCount => _equipmentDefaults.Value.BodySlotData.Length;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheApi"></param>
        public BodyDataRepository(ICacheAPI cacheApi) => _equipmentDefaults = new Lazy<EquipmentDefaults>(() => EquipmentDefaults.Read(cacheApi), LazyThreadSafetyMode.ExecutionAndPublication);

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