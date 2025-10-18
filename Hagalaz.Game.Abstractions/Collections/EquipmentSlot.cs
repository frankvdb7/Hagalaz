namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the different equipment slots on a character.
    /// </summary>
    public enum EquipmentSlot : int
    {
        /// <summary>
        /// An item that is not equippable.
        /// </summary>
        NoSlot = -1,
        /// <summary>
        /// The head slot.
        /// </summary>
        Hat = 0,
        /// <summary>
        /// The back slot.
        /// </summary>
        Cape = 1,
        /// <summary>
        /// The neck slot.
        /// </summary>
        Amulet = 2,
        /// <summary>
        /// The main-hand weapon slot.
        /// </summary>
        Weapon = 3,
        /// <summary>
        /// The torso slot.
        /// </summary>
        Chest = 4,
        /// <summary>
        /// The off-hand slot.
        /// </summary>
        Shield = 5,
        /// <summary>
        /// The legs slot.
        /// </summary>
        Legs = 7,
        /// <summary>
        /// The hands slot.
        /// </summary>
        Hands = 9,
        /// <summary>
        /// The feet slot.
        /// </summary>
        Feet = 10,
        /// <summary>
        /// The ring slot.
        /// </summary>
        Ring = 12,
        /// <summary>
        /// The ammunition slot.
        /// </summary>
        Arrow = 13,
        /// <summary>
        /// The aura slot.
        /// </summary>
        Aura = 14,
    }
}