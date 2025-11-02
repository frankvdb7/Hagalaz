using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    /// <inheritdoc />
    public class EquipmentDefaults : IEquipmentDefaults
    {
        /// <inheritdoc />
        public int Id { get; set; }

        /// <inheritdoc />
        public byte[] BodySlotData { get; set; }

        /// <inheritdoc />
        public int MainHandSlot { get; set; } = -1;

        /// <inheritdoc />
        public int[] WeaponData { get; set; }

        /// <inheritdoc />
        public int[] ShieldData { get; set; }

        /// <inheritdoc />
        public int OffHandSlot { get; set; } = -1;
    }
}
