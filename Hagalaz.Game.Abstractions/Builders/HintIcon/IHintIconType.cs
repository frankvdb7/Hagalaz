using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.HintIcon
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a hint icon where the target
    /// of the icon must be specified (either a location or an entity).
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IHintIconBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHintIconType
    {
        /// <summary>
        /// Specifies that the hint icon should target a specific location in the game world.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> for the hint icon to point to.</param>
        /// <returns>The next step in the fluent builder chain, allowing for location-specific optional parameters.</returns>
        IHintIconLocationOptional AtLocation(ILocation location);

        /// <summary>
        /// Specifies that the hint icon should target a specific entity in the game world.
        /// </summary>
        /// <param name="entity">The <see cref="IEntity"/> for the hint icon to point to.</param>
        /// <returns>The next step in the fluent builder chain, allowing for entity-specific optional parameters.</returns>
        IHintIconEntityOptional AtEntity(IEntity entity);
    }
}