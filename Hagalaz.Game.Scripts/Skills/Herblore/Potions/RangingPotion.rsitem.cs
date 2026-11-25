using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains ranging potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [2444, 169, 171, 173])]
    public class RangingPotion : IngredientPotion
    {
        public RangingPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IHerbloreService herbloreService)
            : base(potionSkillService, herbloreSkillService, herbloreService)
        {
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