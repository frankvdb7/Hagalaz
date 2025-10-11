namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines the contract for a temporary, non-interactive graphical effect.
    /// </summary>
    public interface IGraphic
    {
        /// <summary>
        /// Gets the unique identifier for the graphical effect.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the delay in game ticks before the graphic is displayed.
        /// </summary>
        int Delay { get; }

        /// <summary>
        /// Gets the height offset at which the graphic is rendered relative to the ground.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Gets the rotation of the graphic.
        /// </summary>
        int Rotation { get; }
    }
}
