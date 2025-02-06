using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    /// <summary>
    /// </summary>
    public class SlayerMasterDialogue : NpcDialogueScript
    {
        public SlayerMasterDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

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
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Good day " + Owner.DisplayName + ",", "how may I help you?");
                return true;
            });

            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue(Owner.HasSlayerTask() ? "How many monsters do I have left?" : "Please give me a task.", TalkingTo.Appearance.CompositeID == 9085 ? "Can you tell me about your skill cape?" : "What do you have in your shop?", "Give me a tip.", "Nothing, nevermind.");
                return true;
            });

            AttachDialogueOptionClickHandler("Can you tell me about your skill cape?", (extraData1, extraData2) =>
            {
                Owner.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.Send2TextChatRight, 0, new SkillCapeDialogue(Owner.ServiceProvider.GetRequiredService<ICharacterContextAccessor>(), StatisticsConstants.Slayer), true, TalkingTo);
                return true;
            });

            AttachDialogueOptionClickHandler("How many monsters do I have left?", (extraData1, extraData2) =>
            {
                var task = Owner.Slayer;
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "You are currently assigned to kill:", task.CurrentTaskName, "Only " + task.CurrentKillCount + " more to go.");
                return true;
            });

            AttachDialogueOptionClickHandler("Please give me a task.", (extraData1, extraData2) =>
            {
                if (Owner.HasSlayerTask())
                {
                    var task = Owner.Slayer;
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "You are currently assigned to kill:", task.CurrentTaskName + ".", "Only " + task.CurrentKillCount + " more to go.");
                }
                else
                {
                    var task = Owner.Slayer;
                    task.AssignNewTask(TalkingTo.Appearance.CompositeID);
                    if (Owner.HasSlayerTask())
                    {
                        StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I am afraid that I do not have any tasks for you.", "Come back later when you have gained", "more combat or Slayer experience.");
                    }
                    else
                    {
                        StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Your new task is to kill:", task.CurrentKillCount + " x " + task.CurrentTaskName + ".");
                    }
                }

                SetStage(2);
                return true;
            });

            AttachDialogueOptionClickHandler("Nothing, nevermind.", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler("What do you have in your shop?", (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I have multiple items for sale.");
                SetStage(3);
                return true;
            });

            AttachDialogueOptionClickHandler("Give me a tip.", (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I currently do not have any tips for you.");
                return true;
            });

            AttachDialogueContinueClickHandler(3, (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueContinueClickHandler(4, (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                new OpenShopEvent(Owner, 11).Send();
                return true;
            });
        }
    }
}