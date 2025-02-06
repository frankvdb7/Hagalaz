using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Clans
{
    /// <summary>
    /// </summary>
    public class MottifScript : WidgetScript
    {
        private readonly IWidgetOptionBuilder _widgetOptionBuilder;

        /// <summary>
        ///     The last tab (166 = Top) (177 = Bottom) (189 = Colour)
        /// </summary>
        private byte _lastTab = 166;

        public MottifScript(ICharacterContextAccessor characterContextAccessor, IWidgetOptionBuilder widgetOptionBuilder) : base(characterContextAccessor)
        {
            _widgetOptionBuilder = widgetOptionBuilder;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetOptions(63, 0, 116, _widgetOptionBuilder.SetRightClickOption(0, true).Value);
            InterfaceInstance.SetOptions(66, 0, 116, _widgetOptionBuilder.SetRightClickOption(0, true).Value);

            if (Owner.Clan.Settings.MottifColourLeftTop != -1)
            {
                Owner.Configurations.SendStandardConfiguration(2094, Owner.Clan.Settings.MottifColourLeftTop);
            }

            if (Owner.Clan.Settings.MottifColourRightBottom != -1)
            {
                Owner.Configurations.SendStandardConfiguration(2095, Owner.Clan.Settings.MottifColourRightBottom);
            }

            if (Owner.Clan.Settings.PrimaryClanColour != -1)
            {
                Owner.Configurations.SendStandardConfiguration(2096, Owner.Clan.Settings.PrimaryClanColour);
            }

            if (Owner.Clan.Settings.SecondaryClanColour != -1)
            {
                Owner.Configurations.SendStandardConfiguration(2097, Owner.Clan.Settings.SecondaryClanColour);
            }

            Owner.Configurations.SendCs2Script(4399, [(1105 << 16) | _lastTab]);

            // top mottif
            InterfaceInstance.AttachClickHandler(66,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        _lastTab = 166;
                        Owner.Clan.Settings.MottifTop = (byte)extraData2;
                        return true;
                    }

                    return false;
                });

            // bottom mottif
            InterfaceInstance.AttachClickHandler(63,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        _lastTab = 177;
                        Owner.Clan.Settings.MottifBottom = (byte)extraData2;
                        return true;
                    }

                    return false;
                });

            // colour 1
            InterfaceInstance.AttachClickHandler(35,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        _lastTab = 189;
                        if (Owner.Clan.Settings.MottifColourLeftTop != -1)
                        {
                            Owner.Configurations.SendStandardConfiguration(2347, Owner.Clan.Settings.MottifColourLeftTop);
                        }

                        var colourScript = Owner.ServiceProvider.GetRequiredService<MottifColourScript>();
                        colourScript.OnSelectedColour = selectedColour =>
                        {
                            if (selectedColour != 0)
                            {
                                Owner.Clan.Settings.MottifColourLeftTop = selectedColour;
                            }

                            Owner.Widgets.OpenWidget(1105, 0, this, false);
                        };
                        Owner.Widgets.OpenWidget(1106, 0, colourScript, false);
                        return true;
                    }

                    return false;
                });

            // colour 2
            InterfaceInstance.AttachClickHandler(80,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        _lastTab = 189;
                        if (Owner.Clan.Settings.MottifColourRightBottom != -1)
                        {
                            Owner.Configurations.SendStandardConfiguration(2347, Owner.Clan.Settings.MottifColourRightBottom);
                        }

                        var colourScript = Owner.ServiceProvider.GetRequiredService<MottifColourScript>();
                        colourScript.OnSelectedColour = selectedColour =>
                        {
                            if (selectedColour != 0)
                            {
                                Owner.Clan.Settings.MottifColourRightBottom = selectedColour;
                            }

                            Owner.Widgets.OpenWidget(1105, 0, this, false);
                        };
                        Owner.Widgets.OpenWidget(1106, 0, colourScript, false);
                        return true;
                    }

                    return false;
                });


            // colour 3
            InterfaceInstance.AttachClickHandler(92,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        _lastTab = 189;
                        if (Owner.Clan.Settings.PrimaryClanColour != -1)
                        {
                            Owner.Configurations.SendStandardConfiguration(2347, Owner.Clan.Settings.PrimaryClanColour);
                        }

                        var colourScript = Owner.ServiceProvider.GetRequiredService<MottifColourScript>();
                        colourScript.OnSelectedColour = selectedColour =>
                        {
                            if (selectedColour != 0)
                            {
                                Owner.Clan.Settings.PrimaryClanColour = selectedColour;
                            }

                            Owner.Widgets.OpenWidget(1105, 0, this, false);
                        };
                        Owner.Widgets.OpenWidget(1106, 0, colourScript, false);
                        return true;
                    }

                    return false;
                });

            // colour 4
            InterfaceInstance.AttachClickHandler(104,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        _lastTab = 189;
                        if (Owner.Clan.Settings.SecondaryClanColour != -1)
                        {
                            Owner.Configurations.SendStandardConfiguration(2347, Owner.Clan.Settings.SecondaryClanColour);
                        }

                        var colourScript = Owner.ServiceProvider.GetRequiredService<MottifColourScript>();
                        colourScript.OnSelectedColour = selectedColour =>
                        {
                            if (selectedColour != 0)
                            {
                                Owner.Clan.Settings.SecondaryClanColour = selectedColour;
                            }

                            Owner.Widgets.OpenWidget(1105, 0, this, false);
                        };
                        Owner.Widgets.OpenWidget(1106, 0, colourScript, false);
                        return true;
                    }

                    return false;
                });

            InterfaceInstance.AttachClickHandler(120,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        var script = Owner.ServiceProvider.GetRequiredService<ClanSetup>();
                        Owner.Widgets.OpenWidget(1096, 0, script, false);
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