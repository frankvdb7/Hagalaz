using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains extreme defence potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [15316, 15317, 15318, 15319])]
    public class ExtremeDefencePotion : ExtremePotion
    {
        public ExtremeDefencePotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = (int)(5 + Math.Floor(0.22 * character.Statistics.LevelForExperience(StatisticsConstants.Defence)));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Defence) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Defence, max, amount);
        }
    }
}