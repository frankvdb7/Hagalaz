namespace Hagalaz.Game.Abstractions.Builders.GameObject
{
    /// <summary>
    /// Defines the contract for a game object builder, which serves as the entry point
    /// for constructing an <see cref="Model.GameObjects.IGameObject"/> using a fluent interface.
    /// </summary>
    public interface IGameObjectBuilder
    {
        /// <summary>
        /// Begins the process of building a new game object.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the game object's ID.</returns>
        public IGameObjectId Create();
    }
}