using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains extreme magic potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [15320, 15321, 15322, 15323])]
    public class ExtremeMagicPotion : Potion
    {
        public ExtremeMagicPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
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
            var amount = 7;
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Magic) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Magic, max, amount);
        }
    }
}