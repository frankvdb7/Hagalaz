using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.Tabs;

namespace Hagalaz.Game.Scripts.Widgets.Options
{
    /// <summary>
    ///     The chat channel setup interface.
    /// </summary>
    public class ChatSetup : WidgetScript
    {
        private readonly IScopedGameMediator _gameMediator;
        private IGameConnectHandle _chatSettingsHandle;

        public ChatSetup(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator) : base(characterContextAccessor)
        {
            _gameMediator = gameMediator;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            _chatSettingsHandle = _gameMediator.ConnectHandler<ProfileValueChanged<bool>>(async (context) =>
            {
                if (context.Message.Key == ProfileConstants.ChatSettingsSplitChat)
                {
                    RefreshChatSettingsSplitChat();
                }
            });

            InterfaceInstance.AttachClickHandler(5, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Widgets.OpenWidget(261, InterfaceInstance.ParentSlot, 1, Owner.ServiceProvider.GetRequiredService<OptionsTab>(), true);
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(41, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.ChatSettingsSplitChat));
                    return true;
                }

                return false;
            });

            RefreshChatSettingsSplitChat();
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            _chatSettingsHandle?.Disconnect();
        }

        private void RefreshChatSettingsSplitChat()
        {
            Owner.Configurations.SendStandardConfiguration(287, Owner.Profile.GetValue<bool>(ProfileConstants.ChatSettingsSplitChat) ? 1 : 0);
        }
    }
}