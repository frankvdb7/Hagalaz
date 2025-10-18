namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// Defines the type of an equipment item, which determines which body parts it covers.
    /// </summary>
    public enum EquipmentType : sbyte
    {
        /// <summary>
        /// A standard, single-slot item.
        /// </summary>
        Normal = -1,
        /// <summary>
        /// A two-handed item that occupies both the main-hand and off-hand slots.
        /// </summary>
        TwoHanded = 5,
        /// <summary>
        /// A full body item that covers both the torso and legs slots.
        /// </summary>
        FullBody = 6,
        /// <summary>
        /// A full hat or helmet that covers both the head and hair slots.
        /// </summary>
        FullHat = 8,
        /// <summary>
        /// A full mask that covers the head, hair, and facial hair slots.
        /// </summary>
        FullMask = 11,
    }
}