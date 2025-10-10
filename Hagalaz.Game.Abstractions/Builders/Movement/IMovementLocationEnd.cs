using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Movement
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a forced movement effect
    /// where the movement's end location must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IMovementBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMovementLocationEnd
    {
        /// <summary>
        /// Sets the end location for the forced movement.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> where the movement will end.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        IMovementOptional WithEnd(ILocation location);
    }
}