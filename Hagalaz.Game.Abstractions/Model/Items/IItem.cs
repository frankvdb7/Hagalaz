namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// 
    /// </summary>
    public interface IItem : IRuneObject, IItemBase
    {
        /// <summary>
        /// Contains item extra data.
        /// </summary>
        long[] ExtraData { get; }
        /// <summary>
        /// Gets the item definition.
        /// </summary>
        /// <value>
        /// The item definition.
        /// </value>
        IItemDefinition ItemDefinition { get; }
        /// <summary>
        /// Gets the equipment definition.
        /// </summary>
        /// <value>
        /// The equipment definition.
        /// </value>
        IEquipmentDefinition EquipmentDefinition { get; }
        /// <summary>
        /// Gets the item script.
        /// </summary>
        /// <value>
        /// The item script.
        /// </value>
        IItemScript ItemScript { get; }
        /// <summary>
        /// Gets the equipment script.
        /// </summary>
        /// <value>
        /// The equipment script.
        /// </value>
        IEquipmentScript EquipmentScript { get; }
        /// <summary>
        /// Get's if this item equals to other item.
        /// </summary>
        /// <param name="otherItem">The other item.</param>
        /// <param name="ignoreCount">Wheter item count should be ignored.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool Equals(IItem otherItem, bool ignoreCount = true);
        /// <summary>
        /// Serializes the data for database storing.
        /// </summary>
        /// <returns>Returns a string for database.</returns>
        string? SerializeExtraData();
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        IItem Clone();
        /// <summary>
        /// Copies the specified new count.
        /// </summary>
        /// <param name="newCount">The new count.</param>
        /// <returns></returns>
        IItem Clone(int newCount);
    }
}
