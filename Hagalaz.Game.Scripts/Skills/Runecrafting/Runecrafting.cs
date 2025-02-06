using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Runecrafting
{
    /// <summary>
    /// </summary>
    public static class Runecrafting
    {

        /// <summary>
        ///     Gets the multiplier.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <returns></returns>
        public static int GetMultiplier(ICharacter character, RunecraftingDto definition)
        {
            var level = character.Statistics.GetSkillLevel(StatisticsConstants.Runecrafting);
            for (var i = definition.LevelCountMultipliers.Length - 1; i >= 0; i--)
            {
                if (definition.LevelCountMultipliers[i] == -1)
                {
                    continue;
                }

                if (level >= definition.LevelCountMultipliers[i])
                {
                    return 2 + i;
                }
            }

            return 1;
        }
    }
}