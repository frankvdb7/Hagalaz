using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Logic.Skills
{
    public class SlayerTaskModifier : IRandomObjectModifier
    {
        public void Apply(RandomObjectContext randomObjectContext)
        {
            if (randomObjectContext is not SlayerTaskContext context)
            {
                return;
            }

            if (context.Character.Statistics.GetSkillLevel(StatisticsConstants.Slayer) < context.Task.LevelRequirement &&
                context.Character.Statistics.FullCombatLevel < context.Task.CombatLevelRequirement)
            {
                randomObjectContext.ModifiedProbability = 0;
            }
        }
    }
}