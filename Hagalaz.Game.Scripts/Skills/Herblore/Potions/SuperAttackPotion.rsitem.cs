using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains super attack potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [2436, 145, 147, 149])]
    public class SuperAttackPotion : IngredientPotion
    {
        public SuperAttackPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        protected override int Ingredient => Herbs.HerbloreConstants.Avantoe;

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = (int)(5 + Math.Floor(0.15 * character.Statistics.LevelForExperience(StatisticsConstants.Attack)));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Attack) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Attack, max, amount);
        }
    }
}