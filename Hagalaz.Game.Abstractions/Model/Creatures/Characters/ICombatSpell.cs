
namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a combat spell, encapsulating its effects, requirements, and casting logic.
    /// </summary>
    public interface ICombatSpell
    {
        /// <summary>
        /// Gets the amount of Magic experience awarded for casting this spell.
        /// </summary>
        /// <returns>The Magic experience gained.</returns>
        double GetMagicExperience();
        /// <summary>
        /// A callback executed when a character sets this spell as their autocast option.
        /// </summary>
        /// <param name="activatedOn">The character who activated autocasting.</param>
        void OnAutoCastingActivation(ICharacter activatedOn);
        /// <summary>
        /// A callback executed when a character deactivates this spell as their autocast option.
        /// </summary>
        /// <param name="deactivatedOn">The character who deactivated autocasting.</param>
        void OnAutoCastingDeactivation(ICharacter deactivatedOn);
        /// <summary>
        /// Gets the casting speed of this spell.
        /// </summary>
        /// <param name="caster">The character casting the spell.</param>
        /// <returns>The casting speed in game ticks.</returns>
        int GetCastingSpeed(ICharacter caster);
        /// <summary>
        /// Gets the maximum attack distance of this spell.
        /// </summary>
        /// <param name="caster">The character casting the spell.</param>
        /// <returns>The maximum distance in tiles.</returns>
        int GetCombatDistance(ICharacter caster);
        /// <summary>
        /// Executes the spell's attack logic against a victim.
        /// </summary>
        /// <param name="caster">The character casting the spell.</param>
        /// <param name="victim">The creature being targeted.</param>
        void PerformAttack(ICharacter caster, ICreature victim);
        /// <summary>
        /// Checks if the character meets all the requirements to cast this spell (e.g., level, runes, equipment).
        /// </summary>
        /// <param name="caster">The character attempting to cast the spell.</param>
        /// <returns><c>true</c> if all requirements are met; otherwise, <c>false</c>.</returns>
        bool CheckRequirements(ICharacter caster);
        /// <summary>
        /// Removes the required items (e.g., runes) from the caster's inventory after a successful cast.
        /// </summary>
        /// <param name="caster">The character who cast the spell.</param>
        void RemoveRequirements(ICharacter caster);
        /// <summary>
        /// Determines whether the caster can attack a specific victim with this spell.
        /// </summary>
        /// <param name="caster">The character attempting to cast the spell.</param>
        /// <param name="victim">The potential target.</param>
        /// <returns><c>true</c> if the attack is possible; otherwise, <c>false</c>.</returns>
        bool CanAttack(ICharacter caster, ICreature victim);
    }
}
