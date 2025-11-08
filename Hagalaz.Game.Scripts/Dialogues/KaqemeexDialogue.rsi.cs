using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Dialogues
{
    /// <summary>
    /// </summary>
    public class KaqemeexDialogue : NpcDialogueScript
    {
        public KaqemeexDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Initializes the dialogue.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0,
                (extraData1, extraData2) =>
                {
                    DefaultOptionDialogue("Talk", "Trade");
                    return true;
                });

            AttachDialogueOptionClickHandler("Talk",
                (extraData1, extraData2) =>
                {
                    var skillCapeDialogue = Owner.ServiceProvider.GetRequiredService<SkillCapeDialogue>();
                    skillCapeDialogue.SkillID = StatisticsConstants.Herblore;
                    Owner.Widgets.OpenDialogue(skillCapeDialogue, false, TalkingTo);
                    return true;
                });

            AttachDialogueOptionClickHandler("Trade",
                (extraData1, extraData2) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    Owner.EventManager.SendEvent(new OpenShopEvent(Owner, 2));
                    return true;
                });
        }
    }
}