using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Animation
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating an animation where the animation ID must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended to be implemented directly. It is part of the fluent API provided by <see cref="IAnimationBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAnimationId
    {
        /// <summary>
        /// Sets the unique identifier for the animation being built.
        /// </summary>
        /// <param name="id">The unique identifier for the animation.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        IAnimationOptional WithId(int id);
    }
}