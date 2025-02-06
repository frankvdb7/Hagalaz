using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Dialogues
{
    /// <summary>
    /// Represents level up dialogue interface.
    /// </summary>
    public class LevelupDialogue : DialogueScript
    {
        /// <summary>
        /// Skill icon config IDS.
        /// </summary>
        private static readonly int[] _skillIcon =
        [
            100000000, 400000000, 200000000, 450000000, 250000000, 500000000,
		    300000000, 1100000000, 1250000000, 1300000000, 1050000000, 1200000000,
		    800000000, 1000000000, 900000000, 650000000, 600000000, 700000000,
		    1400000000, 1450000000, 850000000, 1500000000, 1600000000, 1650000000, 1700000000
        ];

        public LevelupDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        /// Happens when dialogue is opened for character.
        /// </summary>
        public override void OnOpen() { }

        /// <summary>
        /// Happens when dialogue is closed for character.
        /// </summary>
        public override void OnClose() { }

        /// <summary>
        /// Happens when interface has been opened for character.
        /// By default, this method attachs continue click handlers and process the dialogue to stage 0.
        /// </summary>
        public override void OnOpened() { }

        /// <summary>
        /// Display's levelup box in this dialogue interface.
        /// </summary>
        /// <param name="skillID">Id of the skill that was leveled up.</param>
        /// <param name="titleText">Title text that will be showed.</param>
        /// <param name="text">Text that will be showed.</param>
        /// <param name="continueClicked">Delegate which is called when continue is clicked.</param>
        public void DisplayLevelupBox(byte skillID, string titleText, string text, OnDialogueClick continueClicked)
        {
            InterfaceInstance.DrawString(0, titleText);
            InterfaceInstance.DrawString(1, text);

            Owner.Configurations.SendStandardConfiguration(1179, _skillIcon[skillID]);
            Owner.QueueTask(new RsTask(() => Owner.Statistics.RefreshFlashedSkills(), 1));


            if (continueClicked != null)
            {
                InterfaceInstance.AttachClickHandler(3, (componentID, clickType, extraData1, extraData2) =>
                {
                    continueClicked.Invoke(3, extraData1);
                    return true;
                });
            }
        }
    }
}