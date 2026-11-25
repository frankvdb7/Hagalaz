using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains super defence potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [2442, 163, 165, 167])]
    public class SuperDefencePotion : IngredientPotion
    {
        public SuperDefencePotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IHerbloreService herbloreService)
            : base(potionSkillService, herbloreSkillService, herbloreService)
        {
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