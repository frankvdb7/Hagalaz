﻿using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Martin
{
    [NpcScriptMetaData([2270])]
    public class MartinThwait : NpcScriptBase
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
            switch (clickType)
            {
                case NpcClickType.Option3Click:
                    new OpenShopEvent(clicker, 14).Send();
                    break;
                case NpcClickType.Option1Click:
                    var dialogue = clicker.ServiceProvider.GetRequiredService<MartinThwaitDialogue>();
                    clicker.Widgets.OpenDialogue(dialogue, true, Owner);
                    break;
                default:
                    base.OnCharacterClickPerform(clicker, clickType);
                    break;
            }
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}