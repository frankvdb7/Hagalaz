using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.GameObject
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a game object where the object's location must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGameObjectBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGameObjectLocation
    {
        /// <summary>
        /// Sets the location for the game object being built.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> where the game object will be placed.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        public IGameObjectOptional WithLocation(ILocation location);
    }
}