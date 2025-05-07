using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains strength potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [113, 115, 117, 119])]
    public class StrengthPotion : Potion
    {
        public StrengthPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService,
            herbloreSkillService) { }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = (int)(3 + Math.Floor(0.10 * character.Statistics.LevelForExperience(StatisticsConstants.Strength)));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Strength) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Strength, max, amount);
        }
    }
}