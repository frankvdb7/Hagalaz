namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// Defines the different types of attack bonuses, corresponding to various combat styles.
    /// </summary>
    public enum AttackBonus
    {
        /// <summary>
        /// No specific attack bonus.
        /// </summary>
        None = 0,
        /// <summary>
        /// The bonus for stab-based melee attacks.
        /// </summary>
        Stab = 1,
        /// <summary>
        /// The bonus for slash-based melee attacks.
        /// </summary>
        Slash = 2,
        /// <summary>
        /// The bonus for crush-based melee attacks.
        /// </summary>
        Crush = 3,
        /// <summary>
        /// The bonus for ranged attacks.
        /// </summary>
        Ranged = 4,
        /// <summary>
        /// The bonus for magic attacks.
        /// </summary>
        Magic = 5,
        /// <summary>
        /// The bonus for attacks related to the Summoning skill.
        /// </summary>
        Summoning = 6,
    }
}
