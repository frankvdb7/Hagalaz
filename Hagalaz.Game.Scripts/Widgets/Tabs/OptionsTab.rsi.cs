using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.Options;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    ///     Represents the option tab.
    /// </summary>
    public class OptionsTab : WidgetScript
    {
        private readonly IScopedGameMediator _gameMediator;
        private IGameConnectHandle _gameConnectHandle;

        public OptionsTab(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator) : base(characterContextAccessor)
        {
            _gameMediator = gameMediator;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            _gameConnectHandle = _gameMediator.ConnectHandler<ProfileValueChanged<bool>>(async (context) =>
            {
                if (context.Message.Key == ProfileConstants.ChatSettingsEffects)
                {
                    RefreshChatEffects();
                }
                if (context.Message.Key == ProfileConstants.ReportSettingsRightClick)
                {
                    RefreshRightClickReporting();
                }
                if (context.Message.Key == ProfileConstants.MouseSettingsButtons)
                {
                    RefreshMouseButtons();
                }
                if (context.Message.Key == ProfileConstants.ChatSettingsProfanity)
                {
                    RefreshChatProfanity();
                }
                if (context.Message.Key == ProfileConstants.MagicSettingsAcceptAid)
                {
                    RefreshAcceptAid();
                }
            });

            // client settings
            InterfaceInstance.AttachClickHandler(22, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                    Owner.Widgets.OpenWidget(742, 0, defaultScript, true);
                    Owner.Configurations.SendCs2Script(279, []); // enables camera distance
                    return true;
                }

                return false;
            });

            // audio settings
            InterfaceInstance.AttachClickHandler(24, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Widgets.OpenWidget(429, InterfaceInstance.ParentSlot, 1, Owner.ServiceProvider.GetRequiredService<AudioOptions>(), true);
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(6, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.ReportSettingsRightClick));
                    return true;
                }

                return false;
            });

            // toggle profanity filter
            InterfaceInstance.AttachClickHandler(11, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.ChatSettingsProfanity));
                    return true;
                }

                return false;
            });

            // toggle chat effects
            InterfaceInstance.AttachClickHandler(12, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.ChatSettingsEffects));
                    return true;
                }

                return false;
            });

            // chat settings
            InterfaceInstance.AttachClickHandler(13, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Widgets.OpenWidget(982, InterfaceInstance.ParentSlot, 1, Owner.ServiceProvider.GetRequiredService<ChatSetup>(), true);
                    return true;
                }

                return false;
            });

            // toggle mouse buttons
            InterfaceInstance.AttachClickHandler(14, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MouseSettingsButtons));
                    return true;
                }

                return false;
            });

            // toggle accept aid
            InterfaceInstance.AttachClickHandler(15, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.MagicSettingsAcceptAid));
                    return true;
                }

                return false;
            });

            // adventures log
            InterfaceInstance.AttachClickHandler(26, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                    Owner.Widgets.OpenWidget(623, 0, defaultScript, true);
                    return true;
                }

                return false;
            });

            Refresh();
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        public void Refresh()
        {
            RefreshChatEffects();
            RefreshRightClickReporting();
            RefreshMouseButtons();
            RefreshChatProfanity();
            RefreshAcceptAid();
        }

        public void RefreshChatProfanity()
        {
            Owner.Configurations.SendStandardConfiguration(1438, Owner.Profile.GetValue<bool>(ProfileConstants.ChatSettingsProfanity) ? 0 : -1);
        }

        public void RefreshRightClickReporting()
        {
            Owner.Configurations.SendStandardConfiguration(1056, Owner.Profile.GetValue<bool>(ProfileConstants.ReportSettingsRightClick) ? 2 : 0);
        }

        public void RefreshChatEffects()
        {
            Owner.Configurations.SendStandardConfiguration(171, Owner.Profile.GetValue<bool>(ProfileConstants.ChatSettingsEffects) ? 1 : 0);
        }

        public void RefreshMouseButtons()
        {
            Owner.Configurations.SendStandardConfiguration(170, Owner.Profile.GetValue<bool>(ProfileConstants.MouseSettingsButtons) ? 1 : 0);
        }

        public void RefreshAcceptAid()
        {
            Owner.Configurations.SendStandardConfiguration(427, Owner.Profile.GetValue<bool>(ProfileConstants.MagicSettingsAcceptAid) ? 1 : 0);
        }
    }
}