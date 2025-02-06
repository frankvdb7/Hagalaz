using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains attack potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [2428, 121, 123, 125])]
    public class AttackPotion : Potion
    {
        public AttackPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = (int)(3 + Math.Floor(0.10 * character.Statistics.LevelForExperience(StatisticsConstants.Attack)));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Attack) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Attack, (byte)max, (byte)amount);
        }
    }
}