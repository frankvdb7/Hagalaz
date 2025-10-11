namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines the contract for a request to play a single animation sequence.
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// Gets the unique identifier for the animation sequence.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the delay in game ticks before the animation starts playing.
        /// </summary>
        int Delay { get; }

        /// <summary>
        /// Gets the priority of the animation. Higher priority animations can override lower priority ones.
        /// </summary>
        int Priority { get; }
    }
}
