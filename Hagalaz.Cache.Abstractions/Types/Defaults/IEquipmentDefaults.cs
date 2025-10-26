namespace Hagalaz.Cache.Abstractions.Types.Defaults
{
    /// <summary>
    /// Represents body data definition which contains info
    /// about how much body parts character model has and other misc info.
    /// </summary>
    public interface IEquipmentDefaults : IType
    {
        /// <summary>
        /// Contains body data.
        /// </summary>
        /// <value>The parts data.</value>
        byte[] BodySlotData { get; }
        /// <summary>
        /// Gets something with weapon display.
        /// </summary>
        /// <value>Something with weapon display.</value>
        int MainHandSlot { get; }
        /// <summary>
        /// Contains weapon data.
        /// </summary>
        /// <value>The weapon data.</value>
        int[] WeaponData { get; }
        /// <summary>
        /// Contains shield data.
        /// </summary>
        /// <value>The shield data.</value>
        int[] ShieldData { get; }
        /// <summary>
        /// Gets something with shield display.
        /// </summary>
        /// <value>Something with shield display.</value>
        int OffHandSlot { get; }
    }
}
