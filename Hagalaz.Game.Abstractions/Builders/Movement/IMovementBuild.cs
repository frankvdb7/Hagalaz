using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Builders.Movement
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a forced movement effect.
    /// This interface provides the method to construct the final <see cref="IForceMovement"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IMovementBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMovementBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="IForceMovement"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IForceMovement"/> object configured with the specified properties.</returns>
        IForceMovement Build();
    }
}