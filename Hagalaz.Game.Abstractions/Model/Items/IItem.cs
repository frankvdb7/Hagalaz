namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// Defines the contract for a game item, combining its base properties with its data definitions and scripts.
    /// </summary>
    public interface IItem : IRuneObject, IItemBase
    {
        /// <summary>
        /// Gets an array of extra data associated with the item, used for storing custom state.
        /// </summary>
        long[] ExtraData { get; }

        /// <summary>
        /// Gets the data definition for this item.
        /// </summary>
        IItemDefinition ItemDefinition { get; }

        /// <summary>
        /// Gets the equipment-specific data definition for this item.
        /// </summary>
        IEquipmentDefinition EquipmentDefinition { get; }

        /// <summary>
        /// Gets the script that controls the item's general behavior.
        /// </summary>
        IItemScript ItemScript { get; }

        /// <summary>
        /// Gets the script that controls the item's behavior when equipped.
        /// </summary>
        IEquipmentScript EquipmentScript { get; }

        /// <summary>
        /// Checks if this item is equal to another item.
        /// </summary>
        /// <param name="otherItem">The other item to compare with.</param>
        /// <param name="ignoreCount">If set to <c>true</c>, the item count will be ignored during comparison.</param>
        /// <returns><c>true</c> if the items are equal; otherwise, <c>false</c>.</returns>
        bool Equals(IItem otherItem, bool ignoreCount = true);

        /// <summary>
        /// Serializes the item's extra data into a string for database storage.
        /// </summary>
        /// <returns>A string representation of the extra data, or <c>null</c> if there is no data to serialize.</returns>
        string? SerializeExtraData();

        /// <summary>
        /// Creates a new, identical copy of this item.
        /// </summary>
        /// <returns>A new <see cref="IItem"/> instance.</returns>
        IItem Clone();

        /// <summary>
        /// Creates a new copy of this item with a different quantity.
        /// </summary>
        /// <param name="newCount">The quantity for the new item.</param>
        /// <returns>A new <see cref="IItem"/> instance with the specified quantity.</returns>
        IItem Clone(int newCount);
    }
}