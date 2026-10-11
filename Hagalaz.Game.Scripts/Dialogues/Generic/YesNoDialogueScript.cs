using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Dialogues.Generic
{
    /// <summary>
    /// 
    /// </summary>
    public class YesNoDialogueScript : DialogueScript
    {
        /// <summary>
        /// The callback for the answer, consumed before it is invoked and cleared when the dialogue closes.
        /// </summary>
        public Action<bool>? Callback { get; set; }

        /// <summary>
        /// The question.
        /// </summary>
        public string Question { get; set; } = null!;

        public YesNoDialogueScript(ICharacterContextAccessor contextAccessor) : base(contextAccessor)
        {
        }

        /// <summary>
        /// Happens when dialogue is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            StandardOptionDialogue(Question, "Yes.", "No.");
            AttachDialogueOptionClickHandler("Yes.", (extraData1, extraData2) =>
            {
                Complete(true);
                return true;
            });
            AttachDialogueOptionClickHandler("No.", (extraData1, extraData2) =>
            {
                Complete(false);
                return true;
            });
        }

        private void Complete(bool answer)
        {
            var callback = Callback;
            Callback = null;
            callback?.Invoke(answer);
        }

        /// <summary>
        /// Happens when dialogue is closed for character.
        /// </summary>
        public override void OnClose() => Callback = null;
    }
}
