using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Zaros
{
    /// <summary>
    ///     Contains zaros items equipment script.
    /// </summary>
    public class ZarosDechargedArmour : DegradingEquipment
    {
        /// <summary>
        ///     Gets the degraded item identifier.
        /// </summary>
        /// <param name="item">The current.</param>
        /// <returns></returns>
        public override short GetDegradedItemID(IItem item) => (short)(item.Id + 1);

        /// <summary>
        ///     Gets the degration on death factor.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override double GetDegrationOnDeathFactor(IItem item) => 0.80;

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override int GetDegrationTicks(IItem item) => 60000; // // 10 hours - (1 minute = 100 ticks * 60 minutes * 10 hours)

        /// <summary>
        ///     Get's items for which this script is made.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int> GetSuitableItems() =>
        [
            20137,
            20141,
            20145, // torva
            20149,
            20153,
            20157, // pernix
            20161,
            20165,
            20169 // virtus
        ];
    }
}