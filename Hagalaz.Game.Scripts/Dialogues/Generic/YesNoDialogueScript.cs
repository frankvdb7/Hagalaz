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
        /// The callback.
        /// </summary>
        public Action<bool> Callback { get; set; }

        /// <summary>
        /// The question.
        /// </summary>
        public string Question { get; set; }

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
                Callback.Invoke(true);
                return true;
            });
            AttachDialogueOptionClickHandler("No.", (extraData1, extraData2) =>
            {
                Callback.Invoke(false);
                return true;
            });
        }

        /// <summary>
        /// Happens when dialogue is closed for character.
        /// </summary>
        public override void OnClose() { }
    }
}