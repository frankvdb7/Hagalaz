namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a script that contains logic and behavior specific to a player character,
    /// extending the base creature script with character-focused events.
    /// </summary>
    public interface ICharacterScript : ICreatureScript
    {
        /// <summary>
        /// Gets the character that this script is attached to.
        /// </summary>
        ICharacter Character { get; }
        /// <summary>
        /// A callback executed when the character's current action is interrupted.
        /// </summary>
        /// <param name="source">The object or system that caused the interruption.</param>
        void OnInterrupt(object source);
        /// <summary>
        /// A callback executed when the character is first registered and added to the game world.
        /// </summary>
        void OnRegistered();
        /// <summary>
        /// A callback executed when the character is permanently destroyed.
        /// </summary>
        void OnDestroy();
        /// <summary>
        /// Determines whether a specific skull icon can be rendered on the character.
        /// </summary>
        /// <param name="icon">The skull icon to be checked.</param>
        /// <returns><c>true</c> if the skull can be rendered; otherwise, <c>false</c>.</returns>
        bool CanRenderSkull(SkullIcon icon);
        /// <summary>
        /// Checks if the script is currently making the character busy, preventing other actions.
        /// </summary>
        /// <returns><c>true</c> if the character is busy due to this script; otherwise, <c>false</c>.</returns>
        bool IsBusy();
        /// <summary>
        /// A callback executed when the script is removed from the character.
        /// </summary>
        void OnRemove();
    }
}
