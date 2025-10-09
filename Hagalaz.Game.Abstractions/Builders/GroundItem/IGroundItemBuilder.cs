namespace Hagalaz.Game.Abstractions.Builders.GroundItem
{
    /// <summary>
    /// Defines the contract for a ground item builder, which serves as the entry point
    /// for constructing an <see cref="Model.Items.IGroundItem"/> using a fluent interface.
    /// </summary>
    public interface IGroundItemBuilder
    {
        /// <summary>
        /// Begins the process of building a new ground item.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the item to be placed on the ground.</returns>
        IGroundItemOnGround Create();
    }
}