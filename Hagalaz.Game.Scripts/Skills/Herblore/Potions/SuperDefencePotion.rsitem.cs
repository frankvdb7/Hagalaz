﻿using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains super defence potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [2442, 163, 165, 167])]
    public class SuperDefencePotion : Potion
    {
        public SuperDefencePotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
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
            if (used.Id == Herbs.HerbloreConstants.Lantadyme || usedWith.Id == Herbs.HerbloreConstants.Lantadyme)
            {
                var potion = HerbloreSkillService.GetPotionByUnfinishedPotionId(used.Id);
                if (potion != null)
                {
                    return HerbloreSkillService.MakePotion(character, used, usedWith, potion, false);
                }

                potion = HerbloreSkillService.GetPotionByUnfinishedPotionId(usedWith.Id);
                if (potion != null)
                {
                    return HerbloreSkillService.MakePotion(character, usedWith, used, potion, false);
                }
            }

            var items = PotionIds;
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
            var amount = (int)(5 + Math.Floor(0.15 * character.Statistics.LevelForExperience(StatisticsConstants.Defence)));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Defence) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Defence, max, amount);
        }
    }
}