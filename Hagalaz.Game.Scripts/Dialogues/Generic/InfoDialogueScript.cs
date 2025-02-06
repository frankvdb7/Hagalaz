using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Dialogues.Generic
{
    public class InfoDialogueScript : DialogueScript
    {
        /// <summary>
        /// The texts
        /// </summary>
        public string[] Texts { get; set; }

        public InfoDialogueScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) {}

        /// <summary>
        /// Raises the Close event.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        /// Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandlers();
            StandardDialogue(Texts);
        }
    }
}