namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines a context that provides access to a character instance.
    /// </summary>
    public interface ICharacterContext
    {
        /// <summary>
        /// Gets the character associated with this context.
        /// </summary>
        public ICharacter Character { get; }
    }
}
