using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets
{
    /// <summary>
    ///     Represents the chat options box below the chatbox.
    /// </summary>
    public class ChatOptionsBox : WidgetScript
    {
        private readonly IScopedGameMediator _gameMediator;
        private IGameConnectHandle _gameConnectHandle;

        public ChatOptionsBox(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator) : base(characterContextAccessor) => _gameMediator = gameMediator;

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            _gameConnectHandle = _gameMediator.ConnectHandler<ProfileValueChanged<Availability>>((context) =>
            {
                if (context.Message.Key == ProfileConstants.ChatSettingsAssistFilter)
                {
                    RefreshAssistFilter();
                }
                else if (context.Message.Key == ProfileConstants.ChatSettingsClanFilter)
                {
                    RefreshClanFilter();
                }
                else if (context.Message.Key == ProfileConstants.ChatSettingsFriendsFilter) 
                {
                    RefreshFriendsFilter();
                } 
                else if (context.Message.Key == ProfileConstants.ChatSettingsGameFilter)
                {
                    RefreshGameFilter();
                }
            });

            InterfaceInstance.AttachClickHandler(0, (componentID, clickType, extraData1, extraData2) =>
            {
                Availability? value = null;
                if (clickType == ComponentClickType.Option2Click)
                {
                    value = Availability.Everyone;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    value = Availability.Friends;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    value = Availability.Nobody;
                }

                if (value != null)
                {
                    _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.ChatSettingsFriendsFilter, value));
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(14, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                    Owner.Widgets.OpenWidget(594, 0, defaultScript, true);
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(17, (componentID, clickType, extraData1, extraData2) =>
            {
                Availability? value = null;
                if (clickType == ComponentClickType.Option2Click)
                {
                    value = Availability.Everyone;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    value = Availability.Friends;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    value = Availability.Nobody;
                }

                if (value != null)
                {
                    _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.ChatSettingsAssistFilter, value));
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(23, (componentID, clickType, extraData1, extraData2) =>
            {
                Availability? value = null;
                if (clickType == ComponentClickType.Option2Click)
                {
                    value = Availability.Everyone;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    value = Availability.Friends;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    value = Availability.Nobody;
                }

                if (value != null)
                {
                    _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.ChatSettingsClanFilter, value));
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(32, (componentID, clickType, extraData1, extraData2) =>
            {
                Availability? value = null;
                if (clickType == ComponentClickType.Option2Click)
                {
                    value = Availability.Everyone;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    value = Availability.Friends;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    value = Availability.Nobody;
                }

                if (value != null)
                {
                    _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.ChatSettingsGameFilter, value));
                }
                return false;
            });

            Refresh();
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() => _gameConnectHandle.Disconnect();

        public void Refresh()
        {
            RefreshAssistFilter();
            RefreshClanFilter();
            RefreshFriendsFilter();
            RefreshGameFilter();
        }

        private void RefreshAssistFilter() => Owner.Configurations.SendStandardConfiguration(1055, (int)Owner.Profile.GetValue<Availability>(ProfileConstants.ChatSettingsAssistFilter));

        private void RefreshClanFilter() => Owner.Configurations.SendStandardConfiguration(1054, (int)Owner.Profile.GetValue<Availability>(ProfileConstants.ChatSettingsClanFilter));

        private void RefreshFriendsFilter() => Owner.Configurations.SendStandardConfiguration(2159, (int)Owner.Profile.GetValue<Availability>(ProfileConstants.ChatSettingsFriendsFilter));

        private void RefreshGameFilter() => Owner.Configurations.SendStandardConfiguration(1056, (int)Owner.Profile.GetValue<Availability>(ProfileConstants.ChatSettingsGameFilter));
    }
}