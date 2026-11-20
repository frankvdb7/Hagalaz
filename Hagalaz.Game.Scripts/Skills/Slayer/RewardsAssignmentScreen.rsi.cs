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
        private const int SkipAssignmentCost = 30;

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

            OnComponentClick skipAssignment = (componentID, clickType, extraData1, extraData2) =>
            {
                if (!Owner.HasSlayerTask())
                {
                    Owner.SendChatMessage("You do not have a task assigned to you yet.");
                    return false;
                }

                var balanceBefore = Owner.Profile.GetValue<int>(ProfileConstants.SlayerRewardPoints);
                if (!Owner.Slayer.TrySkipAssignment(SkipAssignmentCost))
                {
                    Owner.SendChatMessage($"You need {SkipAssignmentCost} points in order to cancel your current mission!");
                    return false;
                }

                InterfaceInstance.DrawString(19, (balanceBefore - SkipAssignmentCost).ToString());
                Owner.SendChatMessage("Your current mission has been cancelled. Speak to a Slayer master for a new one.");
                return true;
            };
            // Period 742 servers route both the button (23) and its label (26) to the 30-point skip.
            InterfaceInstance.AttachClickHandler(23, skipAssignment);
            InterfaceInstance.AttachClickHandler(26, skipAssignment);
        }
    }
}