namespace Hagalaz.Game.Abstractions.Builders.Teleport
{
    /// <summary>
    /// Defines the contract for a teleport builder, which serves as the entry point
    /// for constructing an <see cref="Model.Creatures.ITeleport"/> object using a fluent interface.
    /// </summary>
    public interface ITeleportBuilder
    {
        /// <summary>
        /// Begins the process of building a new teleport effect.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the teleport's destination coordinates or type.</returns>
        ITeleportType Create();
    }
}