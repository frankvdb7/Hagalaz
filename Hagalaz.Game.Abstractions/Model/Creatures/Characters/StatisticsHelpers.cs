using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Provides static helper methods for calculations related to character statistics and skills.
    /// </summary>
    public static class StatisticsHelpers
    {
        /// <summary>
        /// Calculates the minimum experience required to reach a specific skill level.
        /// </summary>
        /// <param name="level">The target skill level.</param>
        /// <returns>The total experience required for the given level.</returns>
        public static int ExperienceForLevel(byte level)
        {
            double points = 0;
            double output = 0;

            for (var lvl = 1; lvl <= level; lvl++)
            {
                points += Math.Floor(lvl + 300.0 * Math.Pow(2.0, lvl / 7.0));
                if (lvl >= level)
                    return (int)output;
                output = Math.Floor(points / 4);
            }

            return 0;
        }

        /// <summary>
        /// Gets the rate at which life points normalize (e.g., after being boosted above the maximum).
        /// </summary>
        /// <returns>The life points normalization rate.</returns>
        public static int GetLifePointsNormalizeRate() => 10;

        /// <summary>
        /// Gets the rate at which life points restore over time.
        /// </summary>
        /// <returns>The life points restore rate.</returns>
        public static int GetLifePointsRestoreRate() => 10;

        /// <summary>
        /// Gets the rate at which poison deals damage.
        /// </summary>
        /// <returns>The poison damage rate in game ticks.</returns>
        public static int GetPoisonRate() => 30;

        /// <summary>
        /// Gets the rate at which run energy drains while running.
        /// </summary>
        /// <returns>The run energy drain rate.</returns>
        public static int GetRunEnergyDrainRate() => 400;

        /// <summary>
        /// Gets the rate at which special attack energy restores over time.
        /// </summary>
        /// <returns>The special energy restore rate in game ticks.</returns>
        public static int GetSpecialEnergyRestoreRate() => 50;

        /// <summary>
        /// Gets the rate at which drained or boosted stats return to their base level.
        /// </summary>
        /// <returns>The statistics normalization rate in game ticks.</returns>
        public static int GetStatisticsNormalizeRate() => 100;

        /// <summary>
        /// Calculates the skill level for a given amount of experience.
        /// </summary>
        /// <param name="skillID">The ID of the skill.</param>
        /// <param name="exp">The total experience in the skill.</param>
        /// <returns>The calculated skill level.</returns>
        public static byte LevelForExperience(byte skillID, double exp)
        {
            double points = 0;

            var max = skillID == StatisticsConstants.Dungeoneering ? (byte)120 : (byte)99;

            for (byte lvl = 1; lvl < max + 1; lvl++)
            {
                points += Math.Floor(lvl + 300.0 * Math.Pow(2.0, lvl / 7.0));
                var output = (int)Math.Floor(points / 4);
                if (output - 1 >= exp)
                    return lvl;
            }

            return max;
        }
    }
}