namespace Hagalaz.Game.Abstractions.Logic.Loot
{
    /// <summary>
    /// Defines the contract for an item that can be dropped as loot, specifying its ID and quantity range.
    /// </summary>
    public interface ILootItem : ILootObject
    {
        /// <summary>
        /// Gets the unique identifier of the item type.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the minimum number of this item that can be dropped in a single loot instance.
        /// </summary>
        int MinimumCount { get; }

        /// <summary>
        /// Gets the maximum number of this item that can be dropped in a single loot instance.
        /// </summary>
        int MaximumCount { get; }
    }
}
