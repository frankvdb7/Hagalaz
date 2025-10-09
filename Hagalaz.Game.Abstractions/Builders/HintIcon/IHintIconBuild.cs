using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.HintIcon
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a hint icon.
    /// This interface provides the method to construct the final <see cref="IHintIcon"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IHintIconBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHintIconBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="IHintIcon"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IHintIcon"/> object configured with the specified properties.</returns>
        IHintIcon Build();
    }
}