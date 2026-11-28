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

            AttachFilterHandler(0, ProfileConstants.ChatSettingsFriendsFilter);

            InterfaceInstance.AttachClickHandler(14, (_, clickType, _, _) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                    Owner.Widgets.OpenWidget(594, 0, defaultScript, true);
                    return true;
                }

                return false;
            });

            AttachFilterHandler(17, ProfileConstants.ChatSettingsAssistFilter);

            AttachFilterHandler(23, ProfileConstants.ChatSettingsClanFilter);

            AttachFilterHandler(32, ProfileConstants.ChatSettingsGameFilter);

            Refresh();
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() => _gameConnectHandle.Disconnect();

        private void AttachFilterHandler(int component, string profileKey)
        {
            InterfaceInstance.AttachClickHandler(component, (_, clickType, _, _) =>
            {
                var value = clickType switch
                {
                    ComponentClickType.Option2Click => Availability.Everyone,
                    ComponentClickType.Option3Click => Availability.Friends,
                    ComponentClickType.Option4Click => Availability.Nobody,
                    _ => (Availability?)null,
                };

                if (value is { } availability)
                {
                    _gameMediator.Publish(new ProfileSetEnumAction(profileKey, availability));
                }

                return false;
            });
        }

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