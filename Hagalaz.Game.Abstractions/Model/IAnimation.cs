namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Represents a single animation display request.
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// Gets the animation id (id from client).
        /// </summary>
        int Id { get; }
        /// <summary>
        /// Gets the animation delay.
        /// </summary>
        int Delay { get; }
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>
        /// The priority.
        /// </value>
        int Priority { get; }
    }
}
