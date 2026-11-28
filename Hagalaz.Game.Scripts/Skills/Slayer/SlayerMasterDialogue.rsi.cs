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
        private readonly ISlayerService _slayerService;

        public SlayerMasterDialogue(ICharacterContextAccessor characterContextAccessor, ISlayerService slayerService) : base(characterContextAccessor) =>
            _slayerService = slayerService;

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0,
                (_, _) =>
                {
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Good day " + Owner.DisplayName + ",", "how may I help you?");
                    return true;
                });

            AttachDialogueContinueClickHandler(1,
                (_, _) =>
                {
                    DefaultOptionDialogue(Owner.HasSlayerTask() ? "How many monsters do I have left?" : "Please give me a task.",
                        TalkingTo.Appearance.CompositeID == 9085 ? "Can you tell me about your skill cape?" : "What do you have in your shop?",
                        "Give me a tip.",
                        "Nothing, never mind.");
                    return true;
                });

            AttachDialogueOptionClickHandler("Can you tell me about your skill cape?",
                (_, _) =>
                {
                    var dialogue = Owner.ServiceProvider.GetRequiredService<SkillCapeDialogue>();
                    dialogue.SkillID = StatisticsConstants.Slayer;
                    Owner.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.Send2TextChatRight, 0, dialogue, true, TalkingTo);
                    return true;
                });

            AttachDialogueOptionClickHandler("How many monsters do I have left?",
                (_, _) =>
                {
                    ShowAssignedTaskDialogue(includePeriodAfterTaskName: false);
                    return true;
                });

            AttachDialogueOptionClickHandler("Please give me a task.",
                (_, _) =>
                {
                    if (Owner.HasSlayerTask())
                    {
                        ShowAssignedTaskDialogue(includePeriodAfterTaskName: true);
                    }
                    else
                    {
                        var task = Owner.Slayer;
                        task.AssignNewTask(TalkingTo.Appearance.CompositeID);
                        if (Owner.HasSlayerTask())
                        {
                            Owner.QueueTask(async cancellationToken =>
                            {
                                var taskId = task.CurrentTaskId;
                                var slayerTask = await _slayerService.FindSlayerTaskDefinition(taskId);
                                cancellationToken.ThrowIfCancellationRequested();
                                if (slayerTask == null || task.CurrentTaskId != taskId)
                                {
                                    return;
                                }

                                StandardNpcDialogue(TalkingTo,
                                    DialogueAnimations.CalmTalk,
                                    "Your new task is to kill:",
                                    task.CurrentKillCount + " x " + slayerTask.Name + ".");
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
                (_, _) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                });

            AttachDialogueOptionClickHandler("What do you have in your shop?",
                (_, _) =>
                {
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I have multiple items for sale.");
                    SetStage(3);
                    return true;
                });

            AttachDialogueOptionClickHandler("Give me a tip.",
                (_, _) =>
                {
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I currently do not have any tips for you.");
                    return true;
                });

            AttachDialogueContinueClickHandler(3,
                (_, _) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                });

            AttachDialogueContinueClickHandler(4,
                (_, _) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    Owner.EventManager.SendEvent(new OpenShopEvent(Owner, 11));
                    return true;
                });
        }

        private void ShowAssignedTaskDialogue(bool includePeriodAfterTaskName)
        {
            if (!Owner.HasSlayerTask())
            {
                return;
            }

            var task = Owner.Slayer;
            var taskId = task.CurrentTaskId;
            Owner.QueueTask(async cancellationToken =>
            {
                var slayerTask = await _slayerService.FindSlayerTaskDefinition(taskId);
                cancellationToken.ThrowIfCancellationRequested();
                if (slayerTask == null || task.CurrentTaskId != taskId)
                {
                    return;
                }

                var taskName = includePeriodAfterTaskName ? slayerTask.Name + "." : slayerTask.Name;
                StandardNpcDialogue(TalkingTo,
                    DialogueAnimations.CalmTalk,
                    "You are currently assigned to kill:",
                    taskName,
                    "Only " + task.CurrentKillCount + " more to go.");
            });
        }
    }
}
