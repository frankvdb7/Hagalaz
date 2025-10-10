namespace Hagalaz.Game.Abstractions.Builders.Location
{
    /// <summary>
    /// Defines the contract for a location builder, which serves as the entry point
    /// for constructing an <see cref="Model.ILocation"/> object using a fluent interface.
    /// </summary>
    public interface ILocationBuilder
    {
        /// <summary>
        /// Begins the process of building a new location.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the location's X-coordinate.</returns>
        ILocationX Create();
    }
}