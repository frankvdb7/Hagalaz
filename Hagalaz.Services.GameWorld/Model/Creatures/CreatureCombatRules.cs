using System;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Common;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    internal sealed class CreatureCombatRules : ICreatureCombatRules
    {
        public string GetCurseSkillName(BonusPrayerType curseType)
        {
            switch (curseType)
            {
                case BonusPrayerType.CurseAttack:
                    return "Attack";
                case BonusPrayerType.CurseStrength:
                    return "Strength";
                case BonusPrayerType.CurseDefence:
                    return "Defence";
                case BonusPrayerType.CurseRanged:
                    return "Ranged";
                case BonusPrayerType.CurseMagic:
                    return "Magic";
                default:
                    return "Unknown";
            }
        }

        public double CalculateMagicAccuracy(double attackLevel, int magicDefenceLevel, int defenceLevel, double attackBonus, double defenceBonus)
        {
            var effectiveAttack = Math.Round(attackLevel + 8.0 + attackBonus);

            double effectiveDefence = defenceLevel;
            effectiveDefence += 8.0;
            effectiveDefence += defenceBonus;
            effectiveDefence = Math.Round(effectiveDefence);

            var effectiveMagicDefence = Math.Round((double)magicDefenceLevel);
            effectiveMagicDefence *= 0.7;
            effectiveDefence *= 0.3;

            effectiveDefence = Math.Round(effectiveDefence);
            effectiveMagicDefence = Math.Round(effectiveMagicDefence);
            effectiveDefence += effectiveMagicDefence;

            var (attackersRoll, defendersRoll) = CalculateRollsFromEffectiveLevels(effectiveAttack, effectiveDefence, attackBonus, defenceBonus);

            return CalculateAccuracy((int)attackersRoll, (int)defendersRoll);
        }

        public double CalculateMeleeAccuracy(double attackLevel, int defenceLevel, double attackBonus, double defenceBonus)
        {
            var (attackersRoll, defendersRoll) = CalculateRolls(attackLevel, defenceLevel, attackBonus, defenceBonus);
            return CalculateAccuracy(attackersRoll, defendersRoll);
        }

        public double CalculateRangedAccuracy(double attackLevel, int defenceLevel, double attackBonus, double defenceBonus)
        {
            var (attackersRoll, defendersRoll) = CalculateRolls(attackLevel, defenceLevel, attackBonus, defenceBonus);
            return CalculateAccuracy((int)attackersRoll, (int)defendersRoll);
        }

        private (double AttackersRoll, double DefendersRoll) CalculateRolls(
            double attackLevel,
            int defenceLevel,
            double attackBonus,
            double defenceBonus)
        {
            var effectiveAttack = Math.Round(attackLevel + 8.0 + attackBonus);
            var effectiveDefence = Math.Round(defenceLevel + 8.0 + defenceBonus);
            return CalculateRollsFromEffectiveLevels(effectiveAttack, effectiveDefence, attackBonus, defenceBonus);
        }

        private (double AttackersRoll, double DefendersRoll) CalculateRollsFromEffectiveLevels(
            double effectiveAttack,
            double effectiveDefence,
            double attackBonus,
            double defenceBonus)
        {
            var attackersRoll = Math.Round(effectiveAttack * (1.0 + attackBonus / 16.0) * 10.0);
            var defendersRoll = Math.Round(effectiveDefence * (1.0 + defenceBonus / 64.0) * 10.0);
            return (attackersRoll, defendersRoll);
        }

        private double CalculateAccuracy(double attackersRoll, double defendersRoll)
        {
            var accuracy = 0.5;
            if (attackersRoll < defendersRoll)
                accuracy = (attackersRoll - 1.0) / (2.0 * defendersRoll);
            else if (attackersRoll > defendersRoll)
                accuracy = 1.0 - (defendersRoll + 1.0) / (2.0 * attackersRoll);

            return accuracy;
        }

        public int PerformMagicDamageCalculation(int minimum, int maximum, double accuracy)
        {
            return PerformDamageCalculation(minimum, maximum, accuracy, 1.0, 1.75);
        }

        public int PerformMeleeDamageCalculation(int minimum, int maximum, double accuracy)
        {
            return PerformDamageCalculation(minimum, maximum, accuracy, 2.0, 4.0);
        }

        public int PerformRangedDamageCalculation(int minimum, int maximum, double accuracy)
        {
            return PerformDamageCalculation(minimum, maximum, accuracy, 2.0, 4.0);
        }

        private static int PerformDamageCalculation(
            int minimum,
            int maximum,
            double accuracy,
            double minimumRollMultiplier,
            double maximumRollMultiplier)
        {
            if (minimum >= maximum)
                return minimum;
            var bestRoll = -1;
            var minimumRoll = (int)Math.Round(accuracy * minimumRollMultiplier);
            var maximumRoll = (int)Math.Round(accuracy * maximumRollMultiplier);
            var amount = 1;
            if (minimumRoll > 0 && maximumRoll > minimumRoll)
                amount = RandomStatic.Generator.Next(minimumRoll, maximumRoll);
            for (var i = 0; i < amount; i++)
            {
                var roll = RandomStatic.Generator.Next(minimum, maximum);
                if (roll > bestRoll)
                    bestRoll = roll;
            }

            return bestRoll;
        }
    }
}
