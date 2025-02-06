using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    /// <summary>
    /// </summary>
    public class RewardsAssignmentScreen : WidgetScript
    {
        public RewardsAssignmentScreen(ICharacterContextAccessor contextAccessor) : base(contextAccessor)
        {
        }

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
            InterfaceInstance.DrawString(19, Owner.Profile.GetValue<int>(ProfileConstants.SlayerRewardPoints).ToString());

            InterfaceInstance.AttachClickHandler(14, (componentID, clickType, extraData1, extraData2) =>
            {
                Owner.Widgets.OpenWidget(163, 0, Owner.ServiceProvider.GetRequiredService<RewardsLearnScreen>(), true);
                return true;
            });
            InterfaceInstance.AttachClickHandler(15, (componentID, clickType, extraData1, extraData2) =>
            {
                Owner.Widgets.OpenWidget(164, 0, Owner.ServiceProvider.GetRequiredService<RewardsBuyScreen>(), true);
                return true;
            });

            InterfaceInstance.AttachClickHandler(23, (componentID, clickType, extraData1, extraData2) =>
            {
                if (Owner.Profile.GetValue<int>(ProfileConstants.SlayerRewardPoints) > 30)
                {
                    Owner.SendChatMessage("You need 30 points in order to reassign your current mission!");
                    return false;
                }

                if (Owner.HasSlayerTask())
                {
                    var dialogue = Owner.ServiceProvider.GetRequiredService<SlayerMasterDialogue>();
                    dialogue.StandardCharacterDialogue(DialogueAnimations.CalmTalk, "Please give me a task.");
                }
                else
                {
                    Owner.SendChatMessage("You do not have a task assigned to you yet.");
                }

                return true;
            });
        }
    }
}