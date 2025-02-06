using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Logic.Skills
{
    public class FishingLootModifier : IRandomObjectModifier
    {
        public required int RequiredLevel { get; init; }

        public void Apply(RandomObjectContext randomObjectContext)
        {
            if (randomObjectContext is not CharacterLootContext context)
            {
                return;
            }

            if (context.Character.Statistics.GetSkillLevel(StatisticsConstants.Fishing) <  RequiredLevel)
            {
                randomObjectContext.ModifiedProbability = 0;
            }
        }
    }
}