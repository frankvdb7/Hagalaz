using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Ancient
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([
        13889, 13895, // vesta's
        13886, 13892, 13898, // statius's
        13860, 13863, 13869, 13866 // zuriel's
    ])]
    public class AncientDegradedEquipment : DegradingEquipment
    {
        /// <summary>
        ///     Gets the degration ticks for one degradable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override int GetDegrationTicks(IItem item) => 6000; // 6000 ticks = 1 hour

        /// <summary>
        ///     Gets the degration on death factor.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override double GetDegrationOnDeathFactor(IItem item) => 1.0;

        /// <summary>
        ///     Gets the degraded item identifier.
        /// </summary>
        /// <param name="item">The current.</param>
        /// <returns></returns>
        public override int GetDegradedItemID(IItem item) => -1; // degrade to dust
    }
}