using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Extensions;
using Hagalaz.Services.GameWorld.Logic.Random;

namespace Hagalaz.Services.GameWorld.Logic.Loot
{
    public class LootGenerator : ILootGenerator
    {
        private readonly IRandomProvider _randomProvider;

        public LootGenerator() : this(new DefaultRandomProvider())
        {
        }

        public LootGenerator(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider;
        }

        public IReadOnlyList<LootResult<T>> GenerateLoot<T>(LootParams lootParams) where T : ILootItem
        {
            if (!lootParams.Table.Enabled)
            {
                return [];
            }

            // The return value, a list of hit objects
            var results = new List<LootResult<T>>(lootParams.MaxCount);

            // Add all the objects that are hit "Always" to the result
            // Those objects are really added always, no matter what maximum count
            // is set in the table! If there are 5 objects "always", those 5 will
            // drop, even if the count says only 3.
            AddAlwaysDrops(results, lootParams);

            // Now calculate the remaining count, this is the table's count minus the
            // number of always-drops.
            // It is possible, that the remaining drops go below zero, in which case
            // no other objects will be added to the result here.
            var remainingCount = lootParams.MaxCount - results.Count;
            if (lootParams.Table.RandomizeResultCount && remainingCount > 0)
            {
                remainingCount = _randomProvider.Next(0, remainingCount + 1); // randomize the total drop count, to allow for no drops, or fewer drops.
            }

            // Continue only, if there is a real drop count left to be processed
            if (remainingCount <= 0)
            {
                return results;
            }

            // Find the objects, that can be hit now
            // This is all objects, that are enabled and that have not already been added through the always flag
            var dropCandidates = BuildDropCandidates(lootParams)
                .OrderByDescending(c => c.Probability)
                .ToList();

            if (dropCandidates.Count == 0)
            {
                return results;
            }

            var totalProbability = dropCandidates.Sum(obj => obj.Probability);
            for (var i = 0; i < remainingCount; i++)
            {
                // This is the magic random number that will decide, which object is hit now
                var hitValue = _randomProvider.Next(totalProbability);

                // Find out in a loop which object's probability hits the random value...
                var runningValue = 0.0;
                foreach (var candidate in dropCandidates)
                {
                    // Count up until we find the first item that exceeds the hit value...
                    runningValue += candidate.Probability;
                    if (hitValue < runningValue)
                    {
                        AddToResult(results, candidate, lootParams);
                        break;
                    }
                }
            }

            // Return the set now
            return results;
        }

        private static IEnumerable<DropCandidate> BuildDropCandidates(LootParams lootParams) =>
            from entry in lootParams.Table.Entries
            where entry.Enabled && !entry.Always
            let context = BuildContext(lootParams, entry)
            select new DropCandidate(entry, context.ModifiedProbability, context.ModifiedMinimumCount, context.ModifiedMaximumCount);

        private void AddAlwaysDrops<T>(List<LootResult<T>> results, LootParams lootParams)
            where T : ILootItem
        {
            foreach (var entry in lootParams.Table.Entries.Where(e => e.Enabled && e.Always))
            {
                var context = BuildContext(lootParams, entry);
                AddToResult(results, new DropCandidate(entry, entry.Probability, context.ModifiedMinimumCount, context.ModifiedMaximumCount), lootParams);
            }
        }

        private void AddToResult<T>(List<LootResult<T>> results, DropCandidate drop, LootParams originalParams)
            where T : ILootItem
        {
            switch (drop.Entry)
            {
                case IRandomTable<ILootObject> table:
                    {
                        var nestedParams = originalParams is CharacterLootParams clp
                            ? new CharacterLootParams(table, clp.Character) { MaxCount = table.MaxResultCount }
                            : new LootParams(table) { MaxCount = table.MaxResultCount };
                        results.AddRange(GenerateLoot<T>(nestedParams));
                        break;
                    }
                case T item:
                    {
                        var count = _randomProvider.Next(drop.MinimumCount, drop.MaximumCount + 1);
                        results.Add(new LootResult<T>(item, count));
                        break;
                    }
            }
        }

        private static LootContext BuildContext(LootParams lootParams, ILootObject entry)
        {
            // Create a base context copy.
            var baseContext = lootParams.ToContext(entry);
            var (baseMin, baseMax) = GetDefaultCounts(entry);

            // Create a copy of the base context and initialize with entry values
            var context = baseContext with
            {
                BaseMinimumCount = baseMin,
                ModifiedMinimumCount = baseMin,
                BaseMaximumCount = baseMax,
                ModifiedMaximumCount = baseMax
            };

            foreach (var modifier in lootParams.Table.Modifiers)
            {
                modifier.Apply(context);
            }

            return context;
        }

        private static readonly (int min, int max) _cachedOneCount = (1, 1);
        private static (int min, int max) GetDefaultCounts(ILootObject loot) =>
            loot is ILootItem item ? (item.MinimumCount, item.MaximumCount) : _cachedOneCount;

        private readonly struct DropCandidate(ILootObject entry, double probability, int minimumCount, int maximumCount)
        {
            public readonly ILootObject Entry = entry;
            public readonly double Probability = probability;
            public readonly int MinimumCount = minimumCount;
            public readonly int MaximumCount = maximumCount;
        }
    }
}