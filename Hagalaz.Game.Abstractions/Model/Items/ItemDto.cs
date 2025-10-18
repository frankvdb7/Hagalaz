namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// A data transfer object representing an item with its ID and quantity.
    /// </summary>
    public struct ItemDto : IItemBase
    {
        /// <summary>
        /// Gets or sets the unique identifier of the item.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the quantity of the item.
        /// </summary>
        public int Count { get; set; } = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemDto"/> struct.
        /// </summary>
        public ItemDto()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemDto"/> struct with a specific ID and a count of 1.
        /// </summary>
        /// <param name="id">The item ID.</param>
        public ItemDto(int id) => Id = id;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemDto"/> struct with a specific ID and count.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <param name="count">The item quantity.</param>
        public ItemDto(int id, int count)
        {
            Id = id;
            Count = count;
        }
    }
}