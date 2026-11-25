using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains extreme attack potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [15308, 15309, 15310, 15311])]
    public class ExtremeAttackPotion : ExtremePotion
    {
        public ExtremeAttackPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IHerbloreService herbloreService)
            : base(potionSkillService, herbloreSkillService, herbloreService)
        {
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