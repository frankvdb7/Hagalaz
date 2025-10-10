using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.GameObject
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a game object where the object's ID must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGameObjectBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGameObjectId
    {
        /// <summary>
        /// Sets the unique identifier for the game object being built.
        /// </summary>
        /// <param name="id">The unique identifier for the game object.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the game object's location.</returns>
        public IGameObjectLocation WithId(int id);
    }
}