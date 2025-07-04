using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Zaros
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([
        20135, 20139, 20143, // torva
        20147, 20151, 20155, // pernix
        20159, 20163, 20167 // virtus
    ])]
    public class ZarosChargedArmour : DegradingEquipment
    {
        /// <summary>
        ///     Gets the degraded item identifier.
        /// </summary>
        /// <param name="item">The current.</param>
        /// <returns></returns>
        public override int GetDegradedItemID(IItem item) => item.Id + 2;

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