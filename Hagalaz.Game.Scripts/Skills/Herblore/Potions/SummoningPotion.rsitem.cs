using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData(itemIds: [12140, 12142, 12144, 12146])]
    public class SummoningPotion : Potion
    {
        public SummoningPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the efect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character) => character.Statistics.HealSkill(StatisticsConstants.Summoning, (int)(7 + Math.Floor(0.25 * character.Statistics.LevelForExperience(StatisticsConstants.Summoning))));
    }
}