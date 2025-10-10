using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Builders.Item
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating an item.
    /// This interface provides the method to construct the final <see cref="IItem"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IItemBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IItemBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="IItem"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IItem"/> object configured with the specified properties.</returns>
        IItem Build();
    }
}