using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains defence potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [2432, 133, 135, 137])]
    public class DefencePotion : Potion
    {
        public DefencePotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            var amount = (int)(3 + Math.Floor(0.10 * character.Statistics.LevelForExperience(StatisticsConstants.Defence)));
            var max = character.Statistics.LevelForExperience(StatisticsConstants.Defence) + amount;
            character.Statistics.HealSkill(StatisticsConstants.Defence, (byte)max, (byte)amount);
        }
    }
}