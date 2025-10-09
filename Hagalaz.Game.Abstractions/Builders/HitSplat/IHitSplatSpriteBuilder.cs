using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.HitSplat
{
    /// <summary>
    /// Represents the builder for configuring a single sprite within a hitsplat.
    /// This interface is used in a nested builder pattern within the main hitsplat builder.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IHitSplatBuilder"/>.
    /// It inherits from <see cref="IHitSplatSpriteOptional"/> to provide methods for setting the sprite's properties, such as amount, type, and icon.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHitSplatSpriteBuilder : IHitSplatSpriteOptional
    {
    }
}