using System;
using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.HitSplat
{
    /// <summary>
    /// Represents a step in the fluent builder pattern for creating a hitsplat where a visual sprite must be defined.
    /// This interface allows for chaining multiple sprites together for a single hitsplat event.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IHitSplatBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHitSplatSprite
    {
        /// <summary>
        /// Adds a visual sprite to the hitsplat being built, configured via a nested builder.
        /// </summary>
        /// <param name="builder">An action that configures the hitsplat sprite using the <see cref="IHitSplatSpriteBuilder"/>.</param>
        /// <returns>The next step in the fluent builder chain, allowing for more optional configurations or for another sprite to be added.</returns>
        IHitSplatOptional AddSprite(Action<IHitSplatSpriteBuilder> builder);
    }
}