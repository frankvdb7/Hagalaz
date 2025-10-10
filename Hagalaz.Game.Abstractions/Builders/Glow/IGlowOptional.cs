using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Glow
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a glow effect where optional
    /// parameters like color, delay, and duration can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGlowBuilder"/>.
    /// It also inherits from <see cref="IGlowBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGlowOptional : IGlowBuild
    {
        /// <summary>
        /// Sets the red component of the glow color.
        /// </summary>
        /// <param name="red">The red color component (0-255).</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGlowOptional WithRed(byte red);
        /// <summary>
        /// Sets the green component of the glow color.
        /// </summary>
        /// <param name="green">The green color component (0-255).</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGlowOptional WithGreen(byte green);
        /// <summary>
        /// Sets the blue component of the glow color.
        /// </summary>
        /// <param name="blue">The blue color component (0-255).</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGlowOptional WithBlue(byte blue);
        /// <summary>
        /// Sets the alpha (transparency) component of the glow color.
        /// </summary>
        /// <param name="alpha">The alpha component (0-255), where 0 is fully transparent and 255 is fully opaque.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGlowOptional WithAlpha(byte alpha);
        /// <summary>
        /// Sets the delay before the glow effect starts.
        /// </summary>
        /// <param name="delay">The delay in game ticks.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGlowOptional WithDelay(int delay);
        /// <summary>
        /// Sets the duration of the glow effect.
        /// </summary>
        /// <param name="duration">The duration of the effect in game ticks.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGlowOptional WithDuration(int duration);
    }
}