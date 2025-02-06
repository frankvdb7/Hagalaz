namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    public static class PrayerConstants
    {
        /// <summary>
        /// Contains standart prayers activators.
        /// </summary>
        public static readonly int[] StandardPrayersActivators = new[]
        {
            0, 1, 2, 18, 19, 3, 4, 5, 6, 7, 8, 20, 21, 9, 10, 11, 24, 12, 13, 14, 22, 23, 15, 16, 17, 25, 27, 26, 29, 28,
        };

        /// <summary>
        /// Normal prayers level requirements for prayer id.
        /// </summary>
        public static readonly double[] StandardLevelRequirements = new double[]
        {
            1, 4, 7, 8, 9, 10, 13, 16, 19, 22, 25, 26, 27, 28, 31, 34, 35, 37, 40, 43, 44, 45, 46, 49, 52, 60, 65, 70, 74, 77
        };

        /// <summary>
        /// Contains standard drain amounts.
        /// </summary>
        public static readonly int[] StandardDrainAmounts = new[]
        {
            1200, 1200, 1200, 1200, 1200, 600, 600, 600, 3600, 1800, 1800, 600, 600, 300, 300, 300, 300, 300, 300, 300, 300, 300, 1200, 600, 180, 180, 240, 150, 200, 180
        };

        /// <summary>
        /// Contains standard prayers deactivators.
        /// </summary>
        public static readonly int[][] StandardPrayersDeactivators = new[]
        {
            // Thick skin
            new[]
            {
                5, 12, 25, 27, 28, 29
            },
            // Burst of Strength
            new[]
            {
                3, 4, 6, 11, 12, 14, 20, 21, 25, 27, 28, 29
            },
            new[]
            {
                3, 4, 7, 11, 12, 15, 20, 21, 25, 27, 28, 29
            },
            new[]
            {
                1, 2, 4, 6, 7, 11, 12, 14, 15, 20, 21, 25, 27, 28, 29
            },
            new[]
            {
                1, 2, 3, 6, 7, 11, 12, 14, 15, 20, 21, 25, 27, 28, 29
            },
            new[]
            {
                0, 13, 25, 27, 28, 29
            },
            new[]
            {
                1, 3, 4, 11, 12, 14, 20, 21, 25, 27, 28, 29
            },
            new[]
            {
                2, 3, 4, 11, 12, 15, 20, 21, 25, 27, 28, 29
            },

            // Rapid restore doesn't turn anything off.
            System.Array.Empty<int>(),
            new[]
            {
                26
            },

            // Protect item doesn't turn anything off.
            new int[]
            {
            },
            new[]
            {
                1, 2, 3, 4, 6, 7, 12, 14, 15, 20, 21, 25, 27, 28, 29
            },
            new[]
            {
                1, 2, 3, 4, 6, 7, 11, 14, 15, 20, 21, 25, 27, 28, 29
            },
            new[]
            {
                0, 5, 25, 27, 28, 29
            },
            new[]
            {
                1, 3, 4, 6, 11, 12, 20, 21, 25, 27, 28, 29
            },
            new[]
            {
                2, 3, 4, 7, 11, 12, 20, 21, 25, 27, 28, 29
            },

            // Protect from Summoning doesn't turn anything off.
            System.Array.Empty<int>(),
            new[]
            {
                18, 19, 22, 23, 24
            },
            new[]
            {
                17, 19, 22, 23, 24
            },
            new[]
            {
                17, 18, 22, 23, 24
            },
            new[]
            {
                1, 2, 3, 4, 5, 6, 7, 11, 12, 14, 15, 21, 25, 27, 28, 29
            },
            new[]
            {
                1, 2, 3, 4, 5, 6, 7, 11, 12, 14, 15, 20, 25, 27, 28, 29
            },
            new[]
            {
                16, 17, 18, 19, 23, 24
            },
            new[]
            {
                16, 17, 18, 19, 22, 24
            },
            new[]
            {
                16, 17, 18, 19, 22, 23
            },
            new[]
            {
                0, 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 20, 21, 27, 28, 29
            },
            new[]
            {
                9
            },
            new[]
            {
                0, 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 20, 21, 25, 28, 29
            },
            new[]
            {
                0, 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 20, 21, 25, 27, 29
            },
            new[]
            {
                0, 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 20, 21, 25, 27, 28
            }
        };

        /// <summary>
        /// Curse players level requirements for player id.
        /// </summary>
        public static readonly double[] CursesLevelRequirements = new double[]
        {
            50, 50, 52, 54, 56, 59, 62, 65, 68, 71, 74, 76, 78, 80, 82, 84, 86, 89, 92, 95
        };

        /// <summary>
        /// Contains curses drain amounts.
        /// </summary>
        public static readonly int[] CursesDrainAmounts = new[]
        {
            1800, // protect item , 1 point per 1800 ms
            240, // sap warrior , 1 point per 240 ms
            240, // sap ranger , 1 point per 240 ms
            240, // sap mage , 1 point per 240 ms
            240, // sap spirit , 1 point per 240 ms
            1800, // berserker, 1 point per 1800 ms
            300, // deflect summoning, 1 point per 300 ms
            300, // deflect magic, 1 point per 300 ms
            300, // deflect missles, 1 point per 300 ms
            300, // deflect melee, 1 point per 300 ms
            360, // leech attack, 1 point per 360 ms
            360, // leech ranged, 1 point per 360 ms
            360, // leech magic, 1 point per 360 ms
            360, // leech defence, 1 point per 360 ms
            360, // leech strength, 1 point per 360 ms
            360, // leech energy, 1 point per 360 ms
            360, // leech special attack, 1 point per 360 ms
            1200, // wrath, 1 point per 1200 ms
            200, // soul split, 1 point per 200 ms
            200, // turmoil, 1 point per 200 ms
        };

        /// <summary>
        /// Contains all standart prayers.
        /// </summary>
        public static readonly NormalPrayer[] StandardPrayers = new[]
        {
            NormalPrayer.ThickSkin, NormalPrayer.BurstOfStrength, NormalPrayer.ClarityOfThought, NormalPrayer.SharpEye, NormalPrayer.MysticWill, NormalPrayer.RockSkin, NormalPrayer.SuperhumanStrength, NormalPrayer.ImprovedReflexes, NormalPrayer.RapidRestore, NormalPrayer.RapidHeal, NormalPrayer.ProtectItem, NormalPrayer.HawkEye, NormalPrayer.MysticLore, NormalPrayer.SteelSkin, NormalPrayer.UltimateStrength, NormalPrayer.IncredibleReflexes, NormalPrayer.ProtectFromSummoning, NormalPrayer.ProtectFromMagic, NormalPrayer.ProtectFromRanged, NormalPrayer.ProtectFromMelee, NormalPrayer.EagleEye, NormalPrayer.MysticMight, NormalPrayer.Retribution, NormalPrayer.Redemption, NormalPrayer.Smite, NormalPrayer.Chivalry, NormalPrayer.RapidRenewal, NormalPrayer.Piety, NormalPrayer.Rigour, NormalPrayer.Augury,
        };

        /// <summary>
        /// Contains all curses.
        /// </summary>
        public static readonly AncientCurses[] Curses = new[]
        {
            AncientCurses.ProtectItem, AncientCurses.SapWarrior, AncientCurses.SapRanger, AncientCurses.SapMage, AncientCurses.SapSpirit, AncientCurses.Berserker, AncientCurses.DeflectSummoning, AncientCurses.DeflectMagic, AncientCurses.DeflectMissiles, AncientCurses.DeflectMelee, AncientCurses.LeechAttack, AncientCurses.LeechRanged, AncientCurses.LeechMagic, AncientCurses.LeechDefence, AncientCurses.LeechStrength, AncientCurses.LeechEnergy, AncientCurses.LeechSpecialAttack, AncientCurses.Wrath, AncientCurses.SoulSplit, AncientCurses.Turmoil,
        };
    }
}