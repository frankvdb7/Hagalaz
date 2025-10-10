using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Animation
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating an animation.
    /// This interface provides the method to construct the final <see cref="IAnimation"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended to be implemented directly. It is part of the fluent API provided by <see cref="IAnimationBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAnimationBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="IAnimation"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IAnimation"/> object configured with the specified properties.</returns>
        IAnimation Build();
    }
}