using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Item
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating an item where the item's ID must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IItemBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IItemId
    {
        /// <summary>
        /// Sets the unique identifier for the item being built.
        /// </summary>
        /// <param name="id">The unique identifier for the item.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        IItemOptional WithId(int id);
    }
}