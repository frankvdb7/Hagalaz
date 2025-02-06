using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains restore potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [2430, 127, 129, 131])]
    public class RestorePotion : Potion
    {
        public RestorePotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            for (byte i = 0; i < 6; i++)
            {
                var amount = (int)Math.Floor(0.40 * character.Statistics.LevelForExperience(i));
                character.Statistics.HealSkill(i, (byte)amount);
            }
        }
    }
}