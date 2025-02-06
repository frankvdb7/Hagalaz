using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    /// <summary>
    /// </summary>
    public class RewardsLearnScreen : WidgetScript
    {
        public RewardsLearnScreen(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
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
            InterfaceInstance.DrawString(18, Owner.Profile.GetValue<int>(ProfileConstants.SlayerRewardPoints).ToString());

            InterfaceInstance.AttachClickHandler(14, (componentID, clickType, extraData1, extraData2) =>
            {
                Owner.Widgets.OpenWidget(161, 0, Owner.ServiceProvider.GetRequiredService<RewardsAssignmentScreen>(), true);
                return true;
            });
            InterfaceInstance.AttachClickHandler(15, (componentID, clickType, extraData1, extraData2) =>
            {
                Owner.Widgets.OpenWidget(164, 0, Owner.ServiceProvider.GetRequiredService<RewardsBuyScreen>(), true);
                return true;
            });
        }
    }
}