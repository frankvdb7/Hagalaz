using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
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
                (extraData1, extraData2) =>
                {
                    StandardNpcDialogue(npcDefinition, DialogueAnimations.CalmTalk, "Good day " + Owner.DisplayName + ",", "how may I help you?");
                    return true;
                });

            AttachDialogueContinueClickHandler(1,
                (extraData1, extraData2) =>
                {
                    DefaultOptionDialogue("How am I doing so far?", "Who are you?", "Where are you?", "Got any tips for me?", "That's all thanks.");
                    return true;
                });

            AttachDialogueOptionClickHandler("How am I doing so far?",
                (extraData1, extraData2) =>
                {
                    Owner.QueueTask(async () =>
                    {
                        if (Owner.HasSlayerTask())
                        {
                            var slayerTask = await _slayerService.FindSlayerTaskDefinition(Owner.Slayer.CurrentTaskId);
                            StandardNpcDialogue(npcDefinition,
                                    DialogueAnimations.CalmTalk,
                                    "You are currently assigned to kill:",
                                    slayerTask!.Name,
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
                (extraData1, extraData2) =>
                {
                    StandardNpcDialogue(npcDefinition,
                        DialogueAnimations.CalmTalk,
                        "I am " + npcDefinition.DisplayName + ", your current Slayer Master.",
                        "I can provide you with information about your",
                        "progress and I can give some tips for your current task.");
                    return true;
                });

            AttachDialogueOptionClickHandler("Where are you?",
                (extraData1, extraData2) =>
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
                (extraData1, extraData2) =>
                {
                    StandardNpcDialogue(npcDefinition, DialogueAnimations.CalmTalk, "I currently do not have any tips for you.");
                    return true;
                });

            AttachDialogueOptionClickHandler("That's all thanks.",
                (extraData1, extraData2) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                });

            AttachDialogueContinueClickHandler(3,
                (extraData1, extraData2) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                });
        }
    }
}