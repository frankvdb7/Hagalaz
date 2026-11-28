using System;

namespace Hagalaz.Game.Scripts.Minigames.Barrows
{
    /// <summary>
    ///     Decides which loot tables a chest claim rolls: per reward either one slain brother's armour table or an
    ///     ordinary band table chosen by reward potential. The model is the published reward-potential system (the 742
    ///     cache's 10-bit field in config 453 fits its 1,014 maximum); the rates are not measured on 742
    ///     (see docs/revision-742/reference/minigames-barrows.md).
    /// </summary>
    internal static class BarrowsRewards
    {
        /// <summary>
        ///     Game-object loot table ids (one armour piece each) for Ahrim, Dharok, Guthan, Karil, Torag and Verac.
        /// </summary>
        public static readonly int[] BrotherSetLootTableIds = [36, 37, 38, 39, 40, 41];

        private const int CoinsTable = 42;
        private const int MindTable = 43;
        private const int ChaosTable = 44;
        private const int DeathTable = 45;
        private const int BloodTable = 46;
        private const int BoltRacksTable = 47;
        private const int LoopHalfTable = 48;
        private const int ToothHalfTable = 49;
        private const int DragonMedHelmTable = 50;

        private const int MaximumRolls = 7;
        private const int BaseDenominator = 450;
        private const int DenominatorStep = 58;
        private const int MinimumDenominator = 73;
        private const int MaximumCreatureLevels = 1000;
        private const int PotentialPerBrother = 2;

        // Combat levels of the crypt creatures in the 742 cache (NPC ids 2031-2037).
        private static readonly Dictionary<int, int> CreatureLevels = new()
        {
            [2031] = 52, // Bloodworm
            [2032] = 43, // Crypt rat
            [2033] = 76, // Giant crypt rat
            [2034] = 56, // Crypt spider
            [2035] = 79, // Giant crypt spider
            [2036] = 77, // Skeleton
            [2037] = 77 // Skeleton
        };

        /// <summary>
        ///     Combat level a slain crypt creature adds to the reward potential, or 0 for any other NPC.
        /// </summary>
        public static int CreatureCombatLevel(int npcId) => CreatureLevels.GetValueOrDefault(npcId);

        /// <summary>
        ///     Number of rewards for the given count of slain brothers.
        /// </summary>
        public static int RollCount(int slainBrothers) => Math.Min(slainBrothers + 1, MaximumRolls);

        /// <summary>
        ///     One-in-N chance of an armour hit per reward for the given count of slain brothers.
        /// </summary>
        public static int Denominator(int slainBrothers) => Math.Max(BaseDenominator - DenominatorStep * slainBrothers, MinimumDenominator);

        /// <summary>
        ///     Reward potential: crypt creature combat levels (capped at 1,000) plus 2 per slain brother.
        /// </summary>
        public static int RewardPotential(int creatureLevels, int slainBrothers) =>
            Math.Min(creatureLevels, MaximumCreatureLevels) + PotentialPerBrother * slainBrothers;

        /// <summary>
        ///     The ordinary band table for a roll in [0, potential].
        /// </summary>
        public static int OrdinaryTableId(int roll) => roll switch
        {
            <= 379 => CoinsTable,
            <= 504 => MindTable,
            <= 629 => ChaosTable,
            <= 754 => DeathTable,
            <= 879 => BloodTable,
            <= 1004 => BoltRacksTable,
            <= 1007 => LoopHalfTable,
            <= 1010 => ToothHalfTable,
            _ => DragonMedHelmTable
        };

        /// <summary>
        ///     Returns the loot table id to roll for each reward. A reward is a slain brother's armour table with the armour
        ///     chance, otherwise an ordinary band table; armour is only possible when at least one brother was slain.
        /// </summary>
        /// <param name="slainBrothers">Indexes of the slain standard brothers.</param>
        /// <param name="creatureLevels">Summed combat levels of crypt creatures slain this run.</param>
        /// <param name="next">Returns a random integer in [0, maxExclusive).</param>
        public static IReadOnlyList<int> RollRewardTables(IReadOnlyList<int> slainBrothers, int creatureLevels, Func<int, int> next)
        {
            var tables = new List<int>();
            var potential = RewardPotential(creatureLevels, slainBrothers.Count);
            var denominator = Denominator(slainBrothers.Count);
            for (var reward = 0; reward < RollCount(slainBrothers.Count); reward++)
            {
                if (slainBrothers.Count > 0 && next(denominator) == 0)
                {
                    tables.Add(BrotherSetLootTableIds[slainBrothers[next(slainBrothers.Count)]]);
                }
                else
                {
                    tables.Add(OrdinaryTableId(next(potential + 1)));
                }
            }

            return tables;
        }
    }
}
