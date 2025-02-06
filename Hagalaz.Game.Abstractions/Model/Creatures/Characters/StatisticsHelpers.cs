using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    public static class StatisticsHelpers
    {

        /// <summary>
        /// Gets the experience minimum fr the specified level.
        /// </summary>
        /// <param name="level">The level to get experience minimum for.</param>
        /// <returns>Returns the experience minimum.</returns>
        public static int ExperienceForLevel(byte level)
        {
            double points = 0;
            double output = 0;

            for (var lvl = 1; lvl <= level; lvl++)
            {
                points += Math.Floor(lvl + 300.0 * Math.Pow(2.0, lvl / 7.0));
                if (lvl >= level)
                    return (int)output;
                output = Math.Floor((double)(points / 4));
            }

            return 0;
        }

        /// <summary>
        /// Get's character hitpoints normalize rate.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetLifePointsNormalizeRate() => 10;

        /// <summary>
        /// Get's character hitpoints restore rate.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetLifePointsRestoreRate() => 10;

        /// <summary>
        /// Get's character poison rate.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetPoisonRate() => 30;

        /// <summary>
        /// Gets the run energy drain rate.
        /// </summary>
        /// <returns></returns>
        // TODO - Include weight.
        public static int GetRunEnergyDrainRate() => 400;

        /// <summary>
        /// Get's character special energy restore rate.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetSpecialEnergyRestoreRate() => 50;

        /// <summary>
        /// Get's character statistics restore rate.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetStatisticsNormalizeRate() => 100;

        /// <summary>
        /// Gets the level for the amount of exp for the specified experience.
        /// </summary>
        /// <param name="skillID">The skill Id.</param>
        /// <param name="exp">The exp.</param>
        /// <returns>System.Byte.</returns>
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