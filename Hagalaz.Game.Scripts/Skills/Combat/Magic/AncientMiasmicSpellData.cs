using Hagalaz.Game.Abstractions;
using Hagalaz.Game.Common;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Features;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Utilities;
using Hagalaz.Game;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    public static class AncientMiasmicSpellData
    {
        /// <summary>
        ///     The RUN e_ REQUIREMENTS
        /// </summary>
        public static readonly RuneType[][] RuneRequirements =
        [
            [RuneType.Chaos, RuneType.Earth, RuneType.Soul], [RuneType.Chaos, RuneType.Earth, RuneType.Soul], [RuneType.Blood, RuneType.Earth, RuneType.Soul],
            [RuneType.Blood, RuneType.Earth, RuneType.Soul]
        ];

        /// <summary>
        ///     The RUN e_ AMOUNTS
        /// </summary>
        public static readonly int[][] RuneAmounts = [[2, 1, 1], [4, 2, 2], [2, 3, 3], [4, 4, 4]];

        /// <summary>
        ///     The LEVE l_ REQUIREMENTS
        /// </summary>
        public static readonly int[] LevelRequirements = [61, 73, 85, 97];

        /// <summary>
        ///     The CONFI g_ IDS
        /// </summary>
        public static readonly int[] ConfigIds = [95, 97, 99, 101];

        /// <summary>
        ///     The EXP
        /// </summary>
        public static readonly double[] Exp = [36.0, 42.0, 48.0, 54.0];

        /// <summary>
        ///     The BAS e_ DAMAGE
        /// </summary>
        public static readonly int[] BaseDamage = [180, 240, 280, 352];

        /// <summary>
        ///     The STAF f_ IDS
        /// </summary>
        public static readonly int[] StaffIds = [13867, 13869, 13941, 13943];
    }
}