﻿using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Shops
{
    [NpcScriptMetaData([682])]
    public class RangeStore : NpcScriptBase
    {
        /// <summary>
        ///     Happens when character clicks NPC and then walks to it and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        ///     handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option1Click)
            {
                var skillCapeDialogue = clicker.ServiceProvider.GetRequiredService<SkillCapeDialogue>();
                skillCapeDialogue.SkillID = StatisticsConstants.Ranged;
                clicker.Widgets.OpenDialogue(skillCapeDialogue, true, Owner);
                return;
            }

            if (clickType == NpcClickType.Option3Click)
            {
                new OpenShopEvent(clicker, 7).Send();
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}