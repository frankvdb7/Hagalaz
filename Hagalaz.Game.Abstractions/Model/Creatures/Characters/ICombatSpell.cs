
namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Contains combat spell.
    /// </summary>
    public interface ICombatSpell
    {
        /// <summary>
        /// Get's amount of magic experience this spell gives.
        /// </summary>
        /// <returns></returns>
        double GetMagicExperience();
        /// <summary>
        /// Get's called when autocasting is set to this spell.
        /// </summary>
        void OnAutoCastingActivation(ICharacter activatedOn);
        /// <summary>
        /// Get's called when autocasting is unset to this spell.
        /// </summary>
        void OnAutoCastingDeactivation(ICharacter deactivatedOn);
        /// <summary>
        /// Get's speed of this spell.
        /// </summary>
        int GetCastingSpeed(ICharacter caster);
        /// <summary>
        /// Get's combat distance of this spell.
        /// </summary>
        int GetCombatDistance(ICharacter caster);
        /// <summary>
        /// Perform's attack to specific target.
        /// </summary>
        void PerformAttack(ICharacter caster, ICreature victim);
        /// <summary>
        /// Check's if character meet's requirements.
        /// </summary>
        bool CheckRequirements(ICharacter caster);
        /// <summary>
        /// Remove's required items from character's inventory.
        /// Return's if removed sucessfully.
        /// </summary>
        /// <returns></returns>
        void RemoveRequirements(ICharacter caster);
        /// <summary>
        /// Determines whether the caster can attack the specified victim.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        /// <returns></returns>
        bool CanAttack(ICharacter caster, ICreature victim);
    }
}
