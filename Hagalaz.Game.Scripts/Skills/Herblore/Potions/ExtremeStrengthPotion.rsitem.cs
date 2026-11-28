using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains extreme strength potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [15312, 15313, 15314, 15315])]
    public class ExtremeStrengthPotion : ExtremePotion
    {
        public ExtremeStrengthPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IHerbloreService herbloreService)
            : base(potionSkillService, herbloreSkillService, herbloreService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = (int)(5 + Math.Floor(0.22 * character.Statistics.LevelForExperience(StatisticsConstants.Strength)));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Strength) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Strength, max, amount);
        }
    }
}