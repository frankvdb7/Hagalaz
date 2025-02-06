using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Skills
{
    /// <summary>
    /// </summary>
    public class XpCounterSetup : WidgetScript
    {
        public XpCounterSetup(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     The current counter identifier
        /// </summary>
        private int _currentCounterID;

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.AttachClickHandler(18, (componentID, type, extraInfo1, extraInfo2) =>
            {
                if (type == ComponentClickType.LeftClick)
                {
                    Owner.Widgets.CloseWidget(InterfaceInstance);
                    return true;
                }

                return false;
            });
            OnComponentClick counterClicked = (componentID, type, extraInfo1, extraInfo2) =>
            {
                if (type == ComponentClickType.LeftClick)
                {
                    _currentCounterID = componentID - 22;
                    Owner.Configurations.SendStandardConfiguration(2478, _currentCounterID + 1);
                }

                return false;
            };
            for (var i = 22; i <= 24; i++)
            {
                InterfaceInstance.AttachClickHandler(i, counterClicked);
            }

            InterfaceInstance.AttachClickHandler(27, (componentID, type, extraInfo1, extraInfo2) =>
            {
                if (type == ComponentClickType.LeftClick)
                {
                    Owner.Statistics.ToggleXpCounter(_currentCounterID);
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(61, (componentID, type, extraInfo1, extraInfo2) =>
            {
                if (type == ComponentClickType.LeftClick)
                {
                    Owner.Statistics.SetXpCounter(_currentCounterID, 0);
                    return true;
                }

                return false;
            });

            OnComponentClick optionClicked = (componentID, type, extraInfo1, extraInfo2) =>
            {
                if (type == ComponentClickType.LeftClick)
                {
                    var optionID = 0;
                    if (componentID == 33)
                    {
                        optionID = 4;
                    }
                    else if (componentID == 34)
                    {
                        optionID = 2;
                    }
                    else if (componentID == 35)
                    {
                        optionID = 3;
                    }
                    else if (componentID == 42)
                    {
                        optionID = 18;
                    }
                    else if (componentID == 49)
                    {
                        optionID = 11;
                    }
                    else
                    {
                        optionID = componentID >= 56 ? componentID - 27 : componentID - 31;
                    }

                    Owner.Statistics.SetTrackedXpCounter(_currentCounterID, optionID);
                }

                return false;
            };
            for (var i = 31; i <= 57; i++)
            {
                InterfaceInstance.AttachClickHandler(i, optionClicked);
            }
        }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
        }
    }
}