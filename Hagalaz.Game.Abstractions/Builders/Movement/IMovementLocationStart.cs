using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Movement
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a forced movement effect
    /// where the movement's start location must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IMovementBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMovementLocationStart
    {
        /// <summary>
        /// Sets the start location for the forced movement.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> from where the movement will begin.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the movement's end location.</returns>
        IMovementLocationEnd WithStart(ILocation location);
    }
}