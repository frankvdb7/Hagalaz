using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Ancient
{
    /// <summary>
    /// </summary>
    public class AncientEquipment : DegradingEquipment
    {
        /// <summary>
        ///     Gets the degraded item identifier.
        /// </summary>
        /// <param name="item">The current.</param>
        /// <returns></returns>
        public override short GetDegradedItemID(IItem item) => (short)(item.Id + 2);

        /// <summary>
        ///     Gets the degration on death factor.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override double GetDegrationOnDeathFactor(IItem item) => 1.0;

        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override int GetDegrationTicks(IItem item) => 0;

        /// <summary>
        ///     Get's items for which this script is made.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int> GetSuitableItems() =>
        [
            13887, 13893, // vesta's
            13884, 13890, 13896, // statius's
            13858, 13861, 13867, 13864 // zuriel's
        ];
    }
}