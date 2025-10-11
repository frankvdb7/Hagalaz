namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines the contract for a basic game object that has a name.
    /// This serves as a root interface for many other game-related entities.
    /// </summary>
    public interface IRuneObject
    {
        /// <summary>
        /// Gets the name of the object.
        /// </summary>
        string Name { get; }
    }
}
