using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Logic.Skills
{
    public class SlayerTaskGenerator : ISlayerTaskGenerator
    {
        public IReadOnlyList<SlayerTaskResult> GenerateTask(SlayerTaskParams taskParams)
        {
            var taskEntries = taskParams.Table.Entries
                .Where(e => e.Enabled)
                .Select(entry =>
                {
                    var context = new SlayerTaskContext(entry)
                    {
                        Character = taskParams.Character
                    };

                    foreach (var modifier in taskParams.Table.Modifiers)
                    {
                        modifier.Apply(context);
                    }

                    return new
                    {
                        Entry = entry, Probability = context.ModifiedProbability
                    };
                })
                .OrderByDescending(item => item.Probability) // Sort by probability (descending)
                .ToList();

            var totalProbability = taskEntries.Sum(entry => entry.Probability);
            var randomValue = Random.Shared.Next(totalProbability);

            List<SlayerTaskResult> results = [];
            var cumulativeProbability = 0.0;
            foreach (var task in taskEntries)
            {
                cumulativeProbability += task.Probability;
                if (!(randomValue < cumulativeProbability))
                {
                    continue;
                }

                results.Add(new SlayerTaskResult(task.Entry, Random.Shared.Next(task.Entry.MinimumCount, task.Entry.MaximumCount + 1)));
                break;
            }

            return results;
        }
    }
}