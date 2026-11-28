using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains super strength potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [2440, 157, 159, 161])]
    public class SuperStrengthPotion : IngredientPotion
    {
        public SuperStrengthPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IHerbloreService herbloreService)
            : base(potionSkillService, herbloreSkillService, herbloreService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = (int)(5 + Math.Floor(0.15 * character.Statistics.LevelForExperience(StatisticsConstants.Strength)));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Strength) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Strength, max, amount);
        }
    }
}