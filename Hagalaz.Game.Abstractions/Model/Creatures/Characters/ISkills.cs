namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a container that provides access to a character's various skill handlers.
    /// </summary>
    public interface ISkills
    {
        /// <summary>
        /// Gets the handler for the character's Magic skill.
        /// </summary>
        IMagic Magic { get; }
        /// <summary>
        /// Gets the handler for the character's Farming skill.
        /// </summary>
        IFarming Farming { get; }
        /// <summary>
        /// Gets the handler for the character's Slayer skill.
        /// </summary>
        ISlayer Slayer { get; }
    }
}
