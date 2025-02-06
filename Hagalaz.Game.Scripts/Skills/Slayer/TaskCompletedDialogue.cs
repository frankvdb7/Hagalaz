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

        public TaskCompletedDialogue(ICharacterContextAccessor contextAccessor, INpcService npcService) : base(contextAccessor)
        {
            _npcService = npcService;
        }

        /// <summary>
        /// Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            Owner.SendChatMessage("Well done " + Owner.DisplayName + "! You have completed your slayer task!");
            AttachDialogueContinueClickHandlers();

            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                var definition = _npcService.FindNpcDefinitionById(Owner.Slayer.SlayerMasterId);
                StandardNpcDialogue(definition.Id, definition.DisplayName, DialogueAnimations.CalmTalk, "Well done " + Owner.DisplayName + "!", "You have completed your task!", "Speak to me for more assignments.");
                return true;
            });

            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
        }

        /// <summary>
        /// Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
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