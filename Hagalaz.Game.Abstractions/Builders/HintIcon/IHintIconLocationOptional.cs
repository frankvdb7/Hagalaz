using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.HintIcon
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a hint icon that targets a location,
    /// allowing for location-specific optional parameters like height, view distance, and direction.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IHintIconBuilder"/>.
    /// It inherits from <see cref="IHintIconOptional"/>, providing access to common optional parameters as well.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHintIconLocationOptional : IHintIconOptional
    {
        /// <summary>
        /// Sets the height at which the hint icon is rendered relative to its location's ground level.
        /// </summary>
        /// <param name="height">The height offset for the hint icon.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHintIconLocationOptional WithHeight(int height);

        /// <summary>
        /// Sets the distance from which the hint icon becomes visible to the player.
        /// </summary>
        /// <param name="viewDistance">The visibility distance for the hint icon.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHintIconLocationOptional WithViewDistance(int viewDistance);

        /// <summary>
        /// Sets the direction the hint icon should point.
        /// </summary>
        /// <param name="direction">The <see cref="HintIconDirection"/> for the icon.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHintIconLocationOptional WithDirection(HintIconDirection direction);
    }
}