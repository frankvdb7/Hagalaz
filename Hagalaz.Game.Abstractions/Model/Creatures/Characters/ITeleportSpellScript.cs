namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    public interface ITeleportSpellScript
    {
        /// <summary>
        /// Gets or sets the magic book.
        /// </summary>
        MagicBook Book { get; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        ILocation Destination { get; }

        /// <summary>
        /// Gets the teleport delay.
        /// </summary>
        /// <value></value>
        int TeleportDelay { get; }

        /// <summary>
        /// Gets the animations.
        /// </summary>
        /// <value></value>
        int[] Animations { get; }

        /// <summary>
        /// Gets the graphics.
        /// </summary>
        /// <value></value>
        int[] Graphics { get; }

        /// <summary>
        /// Gets the magic experience.
        /// TODO - implement this in another class.
        /// </summary>
        /// <value></value>
        double MagicExperience { get; }

        /// <summary>
        /// Gets the teleport distance.
        /// </summary>
        /// <value></value>
        int TeleportDistance { get; }

        /// <summary>
        /// Perform's teleport of the caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        void PerformTeleport(ICharacter caster);

        /// <summary>
        /// Determines whether this instance [can perform teleport] the specified caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        bool CanPerformTeleport(ICharacter caster);

        /// <summary>
        /// Determines whether this instance can teleport the specified caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        bool CanTeleport(ICharacter caster);

        /// <summary>
        /// Called when [teleport started].
        /// </summary>
        /// <param name="caster">The caster.</param>
        void OnTeleportStarted(ICharacter caster);

        /// <summary>
        /// Called when [teleport finished].
        /// </summary>
        void OnTeleportFinished();
    }
}