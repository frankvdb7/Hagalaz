using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.Tabs;

namespace Hagalaz.Game.Scripts.Widgets.Options
{
    /// <summary>
    ///     The chat channel setup interface.
    /// </summary>
    public class AudioOptions : WidgetScript
    {
        public AudioOptions(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen() =>
            InterfaceInstance.AttachClickHandler(18, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Widgets.OpenWidget(261, InterfaceInstance.ParentSlot, 1, Owner.ServiceProvider.GetRequiredService<OptionsTab>(), true);
                    return true;
                }

                return false;
            });

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }
    }
}