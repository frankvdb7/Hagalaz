using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    /// <summary>
    /// 
    /// </summary>
    public class TaskCompletedDialogue : DialogueScript, ISlayerTaskCompletedDialogue
    {
        private readonly INpcService _npcService;
        private readonly ISlayerService _slayerService;

        public TaskCompletedDialogue(ICharacterContextAccessor contextAccessor, INpcService npcService, ISlayerService slayerService) : base(contextAccessor)
        {
            _npcService = npcService;
            _slayerService = slayerService;
        }

        /// <summary>
        /// Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            Owner.SendChatMessage("Well done " + Owner.DisplayName + "! You have completed your slayer task!");
            AttachDialogueContinueClickHandlers();

            AttachDialogueContinueClickHandler(0,
                (extraData1, extraData2) =>
                {
                    Owner.QueueTask(async () =>
                    {
                        var task = await _slayerService.FindSlayerTaskDefinition(Owner.Slayer.CurrentTaskId);
                        if (task == null)
                        {
                            return;
                        }
                        var definition = _npcService.FindNpcDefinitionById(task.SlayerMasterId);
                        StandardNpcDialogue(definition.Id,
                            definition.DisplayName,
                            DialogueAnimations.CalmTalk,
                            "Well done " + Owner.DisplayName + "!",
                            "You have completed your task!",
                            "Speak to me for more assignments.");
                    });
                    return true;
                });

            AttachDialogueContinueClickHandler(1,
                (extraData1, extraData2) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                });
        }

        /// <summary>
        /// Determines whether this instance can be interrupted.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can be interrupted; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanInterrupt() => false;
    }
}