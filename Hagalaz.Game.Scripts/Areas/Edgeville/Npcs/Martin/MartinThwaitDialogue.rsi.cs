using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Dialogues;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Martin
{
    /// <summary>
    /// </summary>
    public class MartinThwaitDialogue : NpcDialogueScript
    {
        public MartinThwaitDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Greetings adventurer!", "What do you want from me?");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("I would like to talk about fencing goods.", "I want to talk about your skill cape.", "Nothing, thanks.");
                return true;
            });
            AttachDialogueOptionClickHandler("I would like to talk about fencing goods.", (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "I would like to talk about fencing goods.");
                AttachDialogueContinueClickHandler(3, (e1, e2) =>
                {
                    var dialogue = Owner.ServiceProvider.GetRequiredService<SellStolenGoodsDialogue>();
                    dialogue.SetStage(1);
                    Owner.Widgets.OpenDialogue(dialogue, false, TalkingTo);
                    dialogue.PerformDialogueOptionClick("Yes, please!");
                    return true;
                });
                return true;
            });
            AttachDialogueOptionClickHandler("I want to talk about your skill cape.", (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "I want to talk about your skill cape.");
                AttachDialogueContinueClickHandler(3, (e1, e2) =>
                {
                    var dialogue = new SkillCapeDialogue.SkillCapeDialogue(Owner.ServiceProvider.GetRequiredService<ICharacterContextAccessor>(), StatisticsConstants.Thieving);
                    dialogue.SetStage(2);
                    Owner.Widgets.OpenDialogue(dialogue, false, TalkingTo);
                    return true;
                });
                return true;
            });
            AttachDialogueOptionClickHandler("Nothing, thanks.", (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Nothing, thanks.");
                AttachDialogueContinueClickHandler(3, (e1, e2) =>
                {
                    InterfaceInstance.Close();
                    return true;
                });
                return true;
            });
        }


        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
        }
    }
}