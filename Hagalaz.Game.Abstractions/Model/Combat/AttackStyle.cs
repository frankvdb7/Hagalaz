namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// Defines the different combat stances or styles a character can use, which affect experience gain and combat calculations.
    /// </summary>
    public enum AttackStyle : int
    {
        /// <summary>
        /// No specific attack style is selected.
        /// </summary>
        None = -1,
        /// <summary>
        /// A melee style that provides an accuracy bonus and grants Attack experience.
        /// </summary>
        MeleeAccurate = 1,
        /// <summary>
        /// A melee style that provides a strength bonus and grants Strength experience.
        /// </summary>
        MeleeAggressive = 2,
        /// <summary>
        /// A melee style that provides a defense bonus and grants Defence experience.
        /// </summary>
        MeleeDefensive = 3,
        /// <summary>
        /// A balanced melee style that grants shared experience across Attack, Strength, and Defence.
        /// </summary>
        MeleeControlled = 4,
        /// <summary>
        /// A ranged style that provides an accuracy bonus and grants Ranged experience.
        /// </summary>
        RangedAccurate = 5,
        /// <summary>
        /// A ranged style that increases attack speed and grants Ranged experience.
        /// </summary>
        RangedRapid = 6,
        /// <summary>
        /// A ranged style that increases attack range and grants shared Ranged and Defence experience.
        /// </summary>
        RangedLongRange = 7,
        /// <summary>
        /// The standard magic attack style, granting Magic experience.
        /// </summary>
        MagicNormal = 8,
        /// <summary>
        /// A magic style that provides a defense bonus and grants shared Magic and Defence experience.
        /// </summary>
        MagicDefensive = 9,
    }

}
