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
