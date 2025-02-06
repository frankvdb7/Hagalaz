using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains super prayer potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [15328, 15329, 15330, 15331])]
    public class SuperPrayerPotion : Potion
    {
        public SuperPrayerPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character) => character.Statistics.HealPrayerPoints((int)(70 + Math.Floor(3.43 * character.Statistics.LevelForExperience(StatisticsConstants.Prayer))));
    }
}