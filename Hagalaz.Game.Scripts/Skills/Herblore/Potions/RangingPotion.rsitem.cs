using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains ranging potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [2444, 169, 171, 173])]
    public class RangingPotion : Potion
    {
        public RangingPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
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
            if (used.Id == Herbs.HerbloreConstants.Grenwallspikes || usedWith.Id == Herbs.HerbloreConstants.Grenwallspikes)
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

            if (PotionIds.Contains(usedWith.Id))
            {
                return PotionSkillService.CombinePotions(character, used, usedWith, PotionIds);
            }

            return false;
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = (int)(4 + Math.Floor(0.10 * character.Statistics.LevelForExperience(StatisticsConstants.Ranged)));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Ranged) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Ranged, max, amount);
        }
    }
}