using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains prayer potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [2434, 139, 141, 143])]
    public class PrayerPotion : Potion
    {
        public PrayerPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character) => character.Statistics.HealPrayerPoints((int)(70 + Math.Floor(2.5 * character.Statistics.LevelForExperience(StatisticsConstants.Prayer))));
    }
}