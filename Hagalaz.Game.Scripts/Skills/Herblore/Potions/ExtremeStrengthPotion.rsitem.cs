using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains extreme strength potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [15312, 15313, 15314, 15315])]
    public class ExtremeStrengthPotion : Potion
    {
        public ExtremeStrengthPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Uses the item.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            var items = PotionIds;
            if (usedWith.Id == items[1] && used.Id == Herbs.HerbloreConstants.Torstol)
            {
                return HerbloreSkillService.MakeOverload(character, used);
            }

            if (used.Id == items[1] && usedWith.Id == Herbs.HerbloreConstants.Torstol)
            {
                return HerbloreSkillService.MakeOverload(character, usedWith);
            }

            if (items.Contains(usedWith.Id))
            {
                return PotionSkillService.CombinePotions(character, used, usedWith, items);
            }

            return false;
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