namespace Hagalaz.Game.Abstractions.Builders.Glow
{
    /// <summary>
    /// Defines the contract for a glow effect builder, which serves as the entry point
    /// for constructing an <see cref="Model.Creatures.IGlow"/> object using a fluent interface.
    /// </summary>
    public interface IGlowBuilder
    {
        /// <summary>
        /// Begins the process of building a new glow effect.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which allows for specifying optional parameters.</returns>
        IGlowOptional Create();
    }
}