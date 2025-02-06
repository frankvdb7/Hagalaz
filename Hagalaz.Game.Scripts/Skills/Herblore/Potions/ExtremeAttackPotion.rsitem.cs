using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains extreme attack potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [15308, 15309, 15310, 15311])]
    public class ExtremeAttackPotion : Potion
    {
        public ExtremeAttackPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
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
            var amount = (int)(5 + Math.Floor(0.22 * character.Statistics.LevelForExperience(StatisticsConstants.Attack)));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Attack) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Attack, max, amount);
        }
    }
}