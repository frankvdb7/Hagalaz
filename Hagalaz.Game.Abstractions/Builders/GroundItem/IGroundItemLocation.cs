using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.GroundItem
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a ground item where the item's location must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGroundItemBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGroundItemLocation
    {
        /// <summary>
        /// Sets the location for the ground item being built.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> where the ground item will be placed.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        public IGroundItemOptional WithLocation(ILocation location);
    }
}