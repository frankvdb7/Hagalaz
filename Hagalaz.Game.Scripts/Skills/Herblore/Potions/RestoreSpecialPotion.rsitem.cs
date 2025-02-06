﻿using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Model;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    ///     Contains restore potion script.
    /// </summary>
    [ItemScriptMetaData(itemIds: [15300, 15301, 15302, 15303])]
    public class RestoreSpecialPotion : Potion
    {
        public RestoreSpecialPotion(IPotionSkillService potionSkillService, IHerbloreSkillService herbloreSkillService) : base(potionSkillService, herbloreSkillService)
        {
        }

        /// <summary>
        ///     Applies the effect.
        /// </summary>
        /// <param name="character">The character.</param>
        protected override void ApplyEffect(ICharacter character)
        {
            if (character.HasState(StateType.RecoverSpecialPotion))
            {
                character.SendChatMessage("You can't restore more special attack yet.");
                return;
            }

            character.Statistics.HealSpecialEnergy((int)(StatisticsConstants.MaximumSpecialEnergy * 0.25));
            character.AddState(new State(StateType.RecoverSpecialPotion, 50)); // 50 ticks = 30 seconds
        }
    }
}