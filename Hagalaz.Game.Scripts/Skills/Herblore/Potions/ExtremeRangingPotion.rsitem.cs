using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains extreme defence potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [15324, 15325, 15326, 15327])]
    public class ExtremeRangingPotion : ExtremePotion
    {
        public ExtremeRangingPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService, IHerbloreService herbloreService)
            : base(potionSkillService, herbloreSkillService, herbloreService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = (int)(4 + Math.Floor(character.Statistics.LevelForExperience(StatisticsConstants.Ranged) / 5.2));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Ranged) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Ranged, max, amount);
        }
    }
}