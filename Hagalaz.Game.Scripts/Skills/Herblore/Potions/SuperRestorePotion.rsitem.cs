using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains super restore potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [3024, 3026, 3028, 3030])]
    public class SuperRestorePotion : Potion
    {
        public SuperRestorePotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            for (var i = 0; i < StatisticsConstants.SkillsCount; i++)
            {
                var amount = (int)Math.Floor(0.33 * character.Statistics.LevelForExperience(i));
                if (i == StatisticsConstants.Prayer)
                {
                    character.Statistics.HealPrayerPoints(amount * 10);
                }
                else
                {
                    character.Statistics.HealSkill(i, amount);
                }
            }
        }
    }
}