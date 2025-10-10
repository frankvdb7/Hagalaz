using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Builders.Glow
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a glow effect.
    /// This interface provides the method to construct the final <see cref="IGlow"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGlowBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGlowBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="IGlow"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IGlow"/> object configured with the specified properties.</returns>
        IGlow Build();
    }
}