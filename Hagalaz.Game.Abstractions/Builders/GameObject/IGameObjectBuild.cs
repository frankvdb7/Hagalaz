using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Abstractions.Builders.GameObject
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a game object.
    /// This interface provides the method to construct the final <see cref="IGameObject"/> instance.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGameObjectBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGameObjectBuild
    {
        /// <summary>
        /// Builds and returns the configured <see cref="IGameObject"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IGameObject"/> configured with the specified properties.</returns>
        IGameObject Build();
    }
}