namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a teleportation spell, encapsulating its destination, effects, and casting logic.
    /// </summary>
    public interface ITeleportSpellScript
    {
        /// <summary>
        /// Gets the magic book this spell belongs to.
        /// </summary>
        MagicBook Book { get; }

        /// <summary>
        /// Gets the destination location for this teleport.
        /// </summary>
        ILocation Destination { get; }

        /// <summary>
        /// Gets the delay in game ticks from the start of the teleport animation to the actual location change.
        /// </summary>
        int TeleportDelay { get; }

        /// <summary>
        /// Gets the animation IDs to be played during the teleport sequence.
        /// </summary>
        int[] Animations { get; }

        /// <summary>
        /// Gets the graphic effect IDs to be displayed during the teleport sequence.
        /// </summary>
        int[] Graphics { get; }

        /// <summary>
        /// Gets the amount of Magic experience awarded for casting this spell.
        /// </summary>
        double MagicExperience { get; }

        /// <summary>
        /// Gets the maximum distance from the destination that the caster can be to perform this teleport.
        /// </summary>
        int TeleportDistance { get; }

        /// <summary>
        /// Executes the teleport logic for the caster.
        /// </summary>
        /// <param name="caster">The character casting the teleport spell.</param>
        void PerformTeleport(ICharacter caster);

        /// <summary>
        /// Checks if the caster meets all requirements to perform the teleport (e.g., levels, runes, location).
        /// </summary>
        /// <param name="caster">The character attempting to cast the spell.</param>
        /// <returns><c>true</c> if the teleport can be performed; otherwise, <c>false</c>.</returns>
        bool CanPerformTeleport(ICharacter caster);

        /// <summary>
        /// A preliminary check to see if the character can generally use this teleport spell.
        /// </summary>
        /// <param name="caster">The character attempting to cast the spell.</param>
        /// <returns><c>true</c> if the character can use this teleport; otherwise, <c>false</c>.</returns>
        bool CanTeleport(ICharacter caster);

        /// <summary>
        /// A callback executed when the teleport sequence begins.
        /// </summary>
        /// <param name="caster">The character who started the teleport.</param>
        void OnTeleportStarted(ICharacter caster);

        /// <summary>
        /// A callback executed when the teleport sequence has finished.
        /// </summary>
        void OnTeleportFinished();
    }
}