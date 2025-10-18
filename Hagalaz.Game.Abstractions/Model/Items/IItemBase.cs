namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// Defines the contract for the most basic properties of an item: its ID and quantity.
    /// </summary>
    public interface IItemBase
    {
        /// <summary>
        /// Gets the unique identifier of the item.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets or sets the quantity of the item.
        /// </summary>
        public int Count { get; set; }
    }
}