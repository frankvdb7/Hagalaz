using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    /// <summary>
    /// </summary>
    public class SlayerShopAndInformation : WidgetScript
    {
        public SlayerShopAndInformation(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen() => Owner.Configurations.SendStandardConfiguration(1233, Owner.Profile.GetValue<int>(ProfileConstants.SlayerRewardPoints));

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
        }
    }
}