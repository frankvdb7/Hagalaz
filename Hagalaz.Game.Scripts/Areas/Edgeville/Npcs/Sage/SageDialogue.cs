using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Sage
{
    public class SageDialogue : NpcDialogueScript
    {
        public SageDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Hello again " + Owner.DisplayName + "!", "Would you like to follow the quick tutorial again?");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Yes, please!", "No, thanks!");
                return true;
            });
            AttachDialogueOptionClickHandler("Yes, please!", (extraData1, extraData2) =>
            {
                var script = Owner.ServiceProvider.GetRequiredService<SageStarterDialogue>();
                Owner.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.Send2TextChatRight, 0, script, false, TalkingTo);
                return false;
            });

            AttachDialogueOptionClickHandler("No, thanks!", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
        }
    }
}