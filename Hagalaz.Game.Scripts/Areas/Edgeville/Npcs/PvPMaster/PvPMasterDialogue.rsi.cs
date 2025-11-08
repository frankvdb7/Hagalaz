using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.PvPMaster
{
    /// <summary>
    ///     Contains Nastroth Dialogue.
    /// </summary>
    public class PvPMasterDialogue : NpcDialogueScript
    {
        public PvPMasterDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Called when [dialogue open].
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
                var script = Owner.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
                Owner.Widgets.OpenDialogue(script, true, Owner);
                return true;
            });

            AttachDialogueOptionClickHandler("Trade", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                Owner.EventManager.SendEvent(new OpenShopEvent(Owner, 25));
                return true;
            });
        }
    }
}