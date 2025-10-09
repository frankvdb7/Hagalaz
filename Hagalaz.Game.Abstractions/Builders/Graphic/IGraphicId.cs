using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Graphic
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a graphic effect where the graphic's ID must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGraphicBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGraphicId
    {
        /// <summary>
        /// Sets the unique identifier for the graphic effect being built.
        /// </summary>
        /// <param name="id">The unique identifier for the graphic.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        IGraphicOptional WithId(int id);
    }
}