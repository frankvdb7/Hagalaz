namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the contract for a glow effect that can be rendered on a creature, specifying its color and duration.
    /// </summary>
    public interface IGlow
    {
        /// <summary>
        /// Gets the alpha (transparency) component of the glow color.
        /// </summary>
        int Alpha { get; }

        /// <summary>
        /// Gets the red component of the glow color.
        /// </summary>
        int Red { get; }

        /// <summary>
        /// Gets the green component of the glow color.
        /// </summary>
        int Green { get; }

        /// <summary>
        /// Gets the blue component of the glow color.
        /// </summary>
        int Blue { get; }

        /// <summary>
        /// Gets the delay in game ticks before the glow effect starts.
        /// </summary>
        int Delay { get; }

        /// <summary>
        /// Gets the duration of the glow effect in game ticks.
        /// </summary>
        int Duration { get; }
    }
}
