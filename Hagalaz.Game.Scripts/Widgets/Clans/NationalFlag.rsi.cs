using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Clans
{
    /// <summary>
    /// </summary>
    public class NationalFlag : WidgetScript
    {
        private readonly IWidgetOptionBuilder _widgetOptionBuilder;

        /// <summary>
        ///     The last tab (166 = Top) (177 = Bottom) (189 = Colour)
        /// </summary>
        private int _lastTab = 166;

        /// <summary>
        ///     The flag
        /// </summary>
        private int _flag;

        public NationalFlag(ICharacterContextAccessor characterContextAccessor, IWidgetOptionBuilder widgetOptionBuilder) : base(characterContextAccessor)
        {
            _widgetOptionBuilder = widgetOptionBuilder;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            _flag = Owner.Clan.Settings.NationalFlag;
            InterfaceInstance.SetOptions(30, 0, 241, _widgetOptionBuilder.SetRightClickOption(0, true).Value);

            InterfaceInstance.AttachClickHandler(30,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        _flag = extraData2;
                        return true;
                    }

                    return false;
                });

            InterfaceInstance.AttachClickHandler(26,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        Owner.Clan.Settings.NationalFlag = _flag;
                        var script = Owner.ServiceProvider.GetRequiredService<ClanSetup>();
                        Owner.Widgets.OpenWidget(1096, 0, script, false);
                        Owner.Configurations.SendCs2Script(4297, []); // settings tab
                        return true;
                    }

                    return false;
                });
        }

        /// <summary>
        ///     Raises the Close event.
        /// </summary>
        public override void OnClose() { }
    }
}