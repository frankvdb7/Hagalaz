using System;
using System.ComponentModel;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Builders.GroundItem
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a ground item where the underlying item instance must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGroundItemBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGroundItemOnGround
    {
        /// <summary>
        /// Specifies the item to be placed on the ground using an existing <see cref="IItem"/> instance.
        /// </summary>
        /// <param name="item">The item instance to be placed on the ground.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the ground item's location.</returns>
        IGroundItemLocation WithItem(IItem item);
        /// <summary>
        /// Specifies the item to be placed on the ground by building it with a nested <see cref="IItemBuilder"/>.
        /// </summary>
        /// <param name="itemBuilder">A function that configures the item using the item builder.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the ground item's location.</returns>
        IGroundItemLocation WithItem(Func<IItemBuilder, IItemBuild> itemBuilder);
    }
}