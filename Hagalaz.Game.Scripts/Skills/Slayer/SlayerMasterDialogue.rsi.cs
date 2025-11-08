using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    /// <summary>
    /// </summary>
    public class SlayerMasterDialogue : NpcDialogueScript
    {
        private readonly ISlayerService _slayerService;

        public SlayerMasterDialogue(ICharacterContextAccessor characterContextAccessor, ISlayerService slayerService) : base(characterContextAccessor) =>
            _slayerService = slayerService;

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0,
                (extraData1, extraData2) =>
                {
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Good day " + Owner.DisplayName + ",", "how may I help you?");
                    return true;
                });

            AttachDialogueContinueClickHandler(1,
                (extraData1, extraData2) =>
                {
                    DefaultOptionDialogue(Owner.HasSlayerTask() ? "How many monsters do I have left?" : "Please give me a task.",
                        TalkingTo.Appearance.CompositeID == 9085 ? "Can you tell me about your skill cape?" : "What do you have in your shop?",
                        "Give me a tip.",
                        "Nothing, never mind.");
                    return true;
                });

            AttachDialogueOptionClickHandler("Can you tell me about your skill cape?",
                (extraData1, extraData2) =>
                {
                    var dialogue = Owner.ServiceProvider.GetRequiredService<SkillCapeDialogue>();
                    dialogue.SkillID = StatisticsConstants.Slayer;
                    Owner.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.Send2TextChatRight, 0, dialogue, true, TalkingTo);
                    return true;
                });

            AttachDialogueOptionClickHandler("How many monsters do I have left?",
                (extraData1, extraData2) =>
                {
                    if (Owner.HasSlayerTask())
                    {
                        var task = Owner.Slayer;
                        Owner.QueueTask(async () =>
                        {
                            var slayerTask = await _slayerService.FindSlayerTaskDefinition(task.CurrentTaskId);
                            StandardNpcDialogue(TalkingTo,
                                DialogueAnimations.CalmTalk,
                                "You are currently assigned to kill:",
                                slayerTask!.Name,
                                "Only " + task.CurrentKillCount + " more to go.");
                        });
                    }

                    return true;
                });

            AttachDialogueOptionClickHandler("Please give me a task.",
                (extraData1, extraData2) =>
                {
                    if (Owner.HasSlayerTask())
                    {
                        var task = Owner.Slayer;
                        Owner.QueueTask(async () =>
                        {
                            var slayerTask = await _slayerService.FindSlayerTaskDefinition(task.CurrentTaskId);
                            StandardNpcDialogue(TalkingTo,
                                DialogueAnimations.CalmTalk,
                                "You are currently assigned to kill:",
                                slayerTask!.Name + ".",
                                "Only " + task.CurrentKillCount + " more to go.");
                        });
                    }
                    else
                    {
                        var task = Owner.Slayer;
                        task.AssignNewTask(TalkingTo.Appearance.CompositeID);
                        if (Owner.HasSlayerTask())
                        {
                            Owner.QueueTask(async () =>
                            {
                                var slayerTask = await _slayerService.FindSlayerTaskDefinition(task.CurrentTaskId);
                                StandardNpcDialogue(TalkingTo,
                                    DialogueAnimations.CalmTalk,
                                    "Your new task is to kill:",
                                    task.CurrentKillCount + " x " + slayerTask!.Name + ".");
                            });
                        }
                        else
                        {
                            StandardNpcDialogue(TalkingTo,
                                DialogueAnimations.CalmTalk,
                                "I am afraid that I do not have any tasks for you.",
                                "Come back later when you have gained",
                                "more combat or Slayer experience.");
                        }
                    }

                    SetStage(2);
                    return true;
                });

            AttachDialogueOptionClickHandler("Nothing, never mind.",
                (extraData1, extraData2) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                });

            AttachDialogueOptionClickHandler("What do you have in your shop?",
                (extraData1, extraData2) =>
                {
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I have multiple items for sale.");
                    SetStage(3);
                    return true;
                });

            AttachDialogueOptionClickHandler("Give me a tip.",
                (extraData1, extraData2) =>
                {
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I currently do not have any tips for you.");
                    return true;
                });

            AttachDialogueContinueClickHandler(3,
                (extraData1, extraData2) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                });

            AttachDialogueContinueClickHandler(4,
                (extraData1, extraData2) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    Owner.EventManager.SendEvent(new OpenShopEvent(Owner, 11));
                    return true;
                });
        }
    }
}