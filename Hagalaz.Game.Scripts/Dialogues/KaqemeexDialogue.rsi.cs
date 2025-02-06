﻿using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Dialogues
{
    /// <summary>
    /// </summary>
    public class KaqemeexDialogue : NpcDialogueScript
    {
        public KaqemeexDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Initializes the dialogue.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Talk", "Trade");
                return true;
            });

            AttachDialogueOptionClickHandler("Talk", (extraData1, extraData2) =>
            {
                Owner.Widgets.OpenDialogue(new SkillCapeDialogue(Owner.ServiceProvider.GetRequiredService<ICharacterContextAccessor>(), StatisticsConstants.Herblore), false, TalkingTo);
                return true;
            });

            AttachDialogueOptionClickHandler("Trade", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                new OpenShopEvent(Owner, 2).Send();
                return true;
            });
        }
    }
}