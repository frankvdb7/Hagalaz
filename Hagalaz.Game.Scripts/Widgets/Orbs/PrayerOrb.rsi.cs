using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Orbs
{
    /// <summary>
    ///     Represents prayer orb.
    /// </summary>
    public class PrayerOrb : WidgetScript
    {
        public PrayerOrb(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.AttachClickHandler(4, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Prayers.QuickPrayer.SwitchQuickPrayer();
                    return true;
                }

                if (clickType == ComponentClickType.Option2Click)
                {
                    Owner.Prayers.QuickPrayer.SelectingQuickPrayers = !Owner.Prayers.QuickPrayer.SelectingQuickPrayers;
                    return true;
                }

                return false;
            });

            Refresh();
        }

        /// <summary>
        ///     Refreshes this instance.
        /// </summary>
        public void Refresh() => Owner.Statistics.RefreshPrayerPoints();

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }
    }
}