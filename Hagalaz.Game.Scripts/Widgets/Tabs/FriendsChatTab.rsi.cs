using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.FriendsChat;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    ///     Represents the friends chat tab.
    /// </summary>
    public class FriendsChatTab : WidgetScript
    {
        private readonly IScopedGameMediator _gameMediator;
        private IGameConnectHandle _gameConnectHandle = default!;

        public FriendsChatTab(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator) : base(characterContextAccessor)
        {
            _gameMediator = gameMediator;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            _gameConnectHandle = _gameMediator.ConnectHandler<ProfileValueChanged<bool>>(async context =>
            {
                if (context.Message.Key == ProfileConstants.FriendsChatSettingsLootShareToggled)
                {
                    RefreshFriendsChatLootShare();
                }
            });

            // clan setup screen
            InterfaceInstance.AttachClickHandler(31, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Widgets.OpenWidget(1108, 0, Owner.ServiceProvider.GetRequiredService<FriendsChatSetup>(), true);
                    return true;
                }

                return false;
            });

            // toggle loot-share
            InterfaceInstance.AttachClickHandler(19, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.FriendsChatSettingsLootShareToggled));
                    return false;
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
            _gameConnectHandle?.Disconnect();
        }

        private void Refresh()
        {
            RefreshFriendsChatLootShare();
        }

        private void RefreshFriendsChatLootShare()
        {
            Owner.Configurations.SendStandardConfiguration(1083, Owner.Profile.GetValue<bool>(ProfileConstants.FriendsChatSettingsLootShareToggled) ? 1 : 0);
        }
    }
}