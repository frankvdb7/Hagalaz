using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.Model.Widgets
{
    /// <summary>
    /// Default npc dialogue script.
    /// </summary>
    public class DefaultNpcDialogueScript : NpcDialogueScript
    {
        public DefaultNpcDialogueScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        /// Starts the dialogue.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0, (_, _) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Hello " + Owner.DisplayName + "!");
                return true;
            });

            AttachDialogueContinueClickHandler(1, (_, _) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
        }

        /// <summary>
        /// Happens when dialogue is ended for character.
        /// </summary>
        public override void OnClose()
        {
        }
    }
}