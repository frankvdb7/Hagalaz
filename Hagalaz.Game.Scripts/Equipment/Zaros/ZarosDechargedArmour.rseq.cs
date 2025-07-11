﻿using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Zaros
{
    /// <summary>
    ///     Contains zaros items equipment script.
    /// </summary>
    [EquipmentScriptMetaData([
        20137,
        20141,
        20145, // torva
        20149,
        20153,
        20157, // pernix
        20161,
        20165,
        20169 // virtus
    ])]
    public class ZarosDechargedArmour : DegradingEquipment
    {
        /// <summary>
        ///     Gets the degraded item identifier.
        /// </summary>
        /// <param name="item">The current.</param>
        /// <returns></returns>
        public override int GetDegradedItemID(IItem item) => item.Id + 1;

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
    }
}