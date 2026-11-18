using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Scripts.Minigames.Barrows
{
    /// <summary>
    ///     Decides which slain brothers' armour tables are rolled for a chest claim.
    ///     The rates are the later published Barrows model, kept as tunable defaults: they are not verified for
    ///     revision 742 (see docs/revision-742/reference/minigames.md).
    /// </summary>
    internal static class BarrowsRewards
    {
        /// <summary>
        ///     Game-object loot table ids (one armour piece each) for Ahrim, Dharok, Guthan, Karil, Torag and Verac.
        ///     Created by hand in the database (see docs/revision-742/reference/minigames.md).
        /// </summary>
        public static readonly int[] BrotherSetLootTableIds = [36, 37, 38, 39, 40, 41];

        private const int MaximumRolls = 7;
        private const int BaseDenominator = 450;
        private const int DenominatorStep = 58;
        private const int MinimumDenominator = 73;

        /// <summary>
        ///     Number of armour rolls for the given count of slain brothers.
        /// </summary>
        public static int RollCount(int slainBrothers) => Math.Min(slainBrothers + 1, MaximumRolls);

        /// <summary>
        ///     One-in-N chance of an armour hit per roll for the given count of slain brothers.
        /// </summary>
        public static int Denominator(int slainBrothers) => Math.Max(BaseDenominator - DenominatorStep * slainBrothers, MinimumDenominator);

        /// <summary>
        ///     Returns the brother index (0..5) whose armour table is rolled for each hit. Only slain brothers can be hit.
        /// </summary>
        /// <param name="slainBrothers">Indexes of the slain standard brothers.</param>
        /// <param name="next">Returns a random integer in [0, maxExclusive).</param>
        public static IReadOnlyList<int> RollSetBrothers(IReadOnlyList<int> slainBrothers, Func<int, int> next)
        {
            var hits = new List<int>();
            if (slainBrothers.Count == 0)
            {
                return hits;
            }

            var denominator = Denominator(slainBrothers.Count);
            for (var roll = 0; roll < RollCount(slainBrothers.Count); roll++)
            {
                if (next(denominator) == 0)
                {
                    hits.Add(slainBrothers[next(slainBrothers.Count)]);
                }
            }

            return hits;
        }
    }
}
