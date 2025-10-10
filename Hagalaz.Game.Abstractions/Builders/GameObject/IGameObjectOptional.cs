using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Scripts;

namespace Hagalaz.Game.Abstractions.Builders.GameObject
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a game object where optional
    /// parameters like rotation, shape, and scripts can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGameObjectBuilder"/>.
    /// It also inherits from <see cref="IGameObjectBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGameObjectOptional : IGameObjectBuild
    {
        /// <summary>
        /// Sets the rotation for the game object.
        /// </summary>
        /// <param name="rotation">The rotation value (typically 0-3).</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        public IGameObjectOptional WithRotation(int rotation);
        /// <summary>
        /// Sets the shape type for the game object, which defines its interaction and placement rules.
        /// </summary>
        /// <param name="shapeType">The <see cref="ShapeType"/> of the object.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        public IGameObjectOptional WithShape(ShapeType shapeType);
        /// <summary>
        /// Attaches a specific script instance to the game object to define its behavior.
        /// </summary>
        /// <param name="script">An instance of a class that implements <see cref="IGameObjectScript"/>.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        public IGameObjectOptional WithScript(IGameObjectScript script);
        /// <summary>
        /// Attaches a script to the game object by its type. The script will be resolved from the dependency injection container.
        /// </summary>
        /// <typeparam name="TScript">The type of the script, which must implement <see cref="IGameObjectScript"/>.</typeparam>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        public IGameObjectOptional WithScript<TScript>() where TScript : IGameObjectScript;
        /// <summary>
        /// Marks the game object as static, meaning it does not despawn or change ownership.
        /// </summary>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        public IGameObjectOptional AsStatic();
    }
}