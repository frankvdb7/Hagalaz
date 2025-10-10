using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Graphic
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a graphic effect.
    /// This interface provides the method to construct the final <see cref="IGraphic"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGraphicBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGraphicBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="IGraphic"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IGraphic"/> object configured with the specified properties.</returns>
        IGraphic Build();
    }
}