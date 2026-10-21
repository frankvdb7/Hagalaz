using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    /// Supplies creature combat calculations and curse skill names.
    /// </summary>
    public interface ICreatureCombatRules
    {
        /// <summary>
        /// Gets the display name associated with a curse bonus type.
        /// </summary>
        string GetCurseSkillName(BonusPrayerType curseType);

        /// <summary>
        /// Calculates magic damage within the supplied bounds.
        /// </summary>
        int PerformMagicDamageCalculation(int minimum, int maximum, double accuracy);

        /// <summary>
        /// Calculates melee damage within the supplied bounds.
        /// </summary>
        int PerformMeleeDamageCalculation(int minimum, int maximum, double accuracy);

        /// <summary>
        /// Calculates ranged damage within the supplied bounds.
        /// </summary>
        int PerformRangedDamageCalculation(int minimum, int maximum, double accuracy);
    }
}
