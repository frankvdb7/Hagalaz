namespace Hagalaz.Game.Abstractions.Builders.Movement
{
    /// <summary>
    /// Defines the contract for a movement builder, which serves as the entry point
    /// for constructing an <see cref="Model.Creatures.IForceMovement"/> object using a fluent interface.
    /// </summary>
    public interface IMovementBuilder
    {
        /// <summary>
        /// Begins the process of building a new forced movement effect.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the movement's starting location.</returns>
        IMovementLocationStart Create();
    }
}