using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Items.Slayer
{
    /// <summary>
    /// </summary>
    public class EnchantedGemDialogue : DialogueScript
    {
        private readonly ISlayerService _slayerService;
        private readonly INpcService _npcService;

        public EnchantedGemDialogue(ICharacterContextAccessor characterContextAccessor, ISlayerService slayerService, INpcService npcService) : base(
            characterContextAccessor)
        {
            _slayerService = slayerService;
            _npcService = npcService;
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() { }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            var currentTask = _slayerService.FindSlayerTaskDefinition(Owner.Slayer.CurrentTaskId).Result;
            if (currentTask == null)
            {
                return;
            }
            var npcDefinition = _npcService.FindNpcDefinitionById(currentTask.SlayerMasterId);

            AttachDialogueContinueClickHandler(0,
                (_, _) =>
                {
                    StandardNpcDialogue(npcDefinition, DialogueAnimations.CalmTalk, "Good day " + Owner.DisplayName + ",", "how may I help you?");
                    return true;
                });

            AttachDialogueContinueClickHandler(1,
                (_, _) =>
                {
                    DefaultOptionDialogue("How am I doing so far?", "Who are you?", "Where are you?", "Got any tips for me?", "That's all thanks.");
                    return true;
                });

            AttachDialogueOptionClickHandler("How am I doing so far?",
                (_, _) =>
                {
                    Owner.QueueTask(async cancellationToken =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (Owner.HasSlayerTask())
                        {
                            var taskId = Owner.Slayer.CurrentTaskId;
                            var slayerTask = await _slayerService.FindSlayerTaskDefinition(taskId);
                            cancellationToken.ThrowIfCancellationRequested();
                            if (slayerTask == null || Owner.Slayer.CurrentTaskId != taskId)
                            {
                                return;
                            }

                            StandardNpcDialogue(npcDefinition,
                                    DialogueAnimations.CalmTalk,
                                    "You are currently assigned to kill:",
                                    slayerTask.Name,
                                    "Only " + Owner.Slayer.CurrentKillCount + " more to go.");
                        }
                        else
                        {
                            StandardNpcDialogue(npcDefinition,
                                DialogueAnimations.CalmTalk,
                                "You do not have a task assigned to you yet.",
                                "Visit me if you want a new task assigned to you.");
                        }
                    });

                    return true;
                });

            AttachDialogueOptionClickHandler("Who are you?",
                (_, _) =>
                {
                    StandardNpcDialogue(npcDefinition,
                        DialogueAnimations.CalmTalk,
                        "I am " + npcDefinition.DisplayName + ", your current Slayer Master.",
                        "I can provide you with information about your",
                        "progress and I can give some tips for your current task.");
                    return true;
                });

            AttachDialogueOptionClickHandler("Where are you?",
                (_, _) =>
                {
                    if (npcDefinition.Id == 8464)
                    {
                        StandardNpcDialogue(npcDefinition,
                            DialogueAnimations.CalmTalk,
                            "I am currently residing in Edgeville, just south of the Edgeville bank.");
                    }
                    else
                    {
                        StandardNpcDialogue(npcDefinition, DialogueAnimations.CalmTalk, "I am currently residing somewhere in Hagalaz,");
                    }

                    return true;
                });

            AttachDialogueOptionClickHandler("Got any tips for me?",
                (_, _) =>
                {
                    StandardNpcDialogue(npcDefinition, DialogueAnimations.CalmTalk, "I currently do not have any tips for you.");
                    return true;
                });

            AttachDialogueOptionClickHandler("That's all thanks.",
                (_, _) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                });

            AttachDialogueContinueClickHandler(3,
                (_, _) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                });
        }
    }
}
