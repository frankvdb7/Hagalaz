using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Builders.HitSplat
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a hitsplat.
    /// This interface provides the method to construct the final <see cref="IHitSplat"/> object.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IHitSplatBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHitSplatBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="IHitSplat"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IHitSplat"/> object configured with the specified properties.</returns>
        IHitSplat Build();
    }
}