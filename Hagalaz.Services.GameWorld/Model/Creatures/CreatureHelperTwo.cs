using System;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Common;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    public static class CreatureHelperTwo
    {
        /// <summary>
        ///     Get's name of the skill by curse type.
        /// </summary>
        /// <param name="curseType">Type of the curse.</param>
        /// <returns>String.</returns>
        public static string GetCurseSkillName(BonusPrayerType curseType)
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

        /// <summary>
        ///     Calculate's random amount of damage between minimum and maximum.
        /// </summary>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="accuracy">The accuracy.</param>
        /// <returns>System.Int32.</returns>
        public static int PerformMagicDamageCalculation(int minimum, int maximum, double accuracy)
        {
            return PerformDamageCalculation(minimum, maximum, accuracy, 1.0, 1.75);
        }

        /// <summary>
        ///     Calculate's random amount of damage between minimum and maximum.
        /// </summary>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="accuracy">The accuracy.</param>
        /// <returns>System.Int32.</returns>
        public static int PerformMeleeDamageCalculation(int minimum, int maximum, double accuracy)
        {
            return PerformDamageCalculation(minimum, maximum, accuracy, 2.0, 4.0);
        }

        /// <summary>
        ///     Calculate's random amount of damage between minimum and maximum.
        /// </summary>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="accuracy">The accuracy.</param>
        /// <returns>System.Int32.</returns>
        public static int PerformRangedDamageCalculation(int minimum, int maximum, double accuracy)
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
