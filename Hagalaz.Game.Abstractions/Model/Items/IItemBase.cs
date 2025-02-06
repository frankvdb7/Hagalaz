namespace Hagalaz.Game.Abstractions.Model.Items
{
    public interface IItemBase
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; }
        /// <summary>
        /// The quantity of the item.
        /// </summary>
        /// <value>The count.</value>
        public int Count { get; set; }
    }
}
