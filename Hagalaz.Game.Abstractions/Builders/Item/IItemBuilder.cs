namespace Hagalaz.Game.Abstractions.Builders.Item
{
    /// <summary>
    /// Defines the contract for an item builder, which serves as the entry point
    /// for constructing an <see cref="Model.Items.IItem"/> object using a fluent interface.
    /// </summary>
    public interface IItemBuilder
    {
        /// <summary>
        /// Begins the process of building a new item.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the item's ID.</returns>
        IItemId Create();
    }
}