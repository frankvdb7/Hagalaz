using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    /// </summary>
    public class LeftClickOptionTab : WidgetScript
    {
        private readonly IScopedGameMediator _gameMediator;
        private IGameConnectHandle _connectHandle = default!;

        public LeftClickOptionTab(ICharacterContextAccessor contextAccessor, IScopedGameMediator gameMediator) : base(contextAccessor) => _gameMediator = gameMediator;

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            _connectHandle = _gameMediator.ConnectHandler<ProfileValueChanged<int>>(async (context) =>
            {
                if (context.Message.Key == ProfileConstants.SummoningSettingsOrbOptionId)
                {
                    Refresh();
                }
            });

            OnComponentClick clickHandler = (componentID, clickType, extra1, extra2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                _gameMediator.Publish(new ProfileSetIntAction(ProfileConstants.SummoningSettingsOrbOptionId, (componentID - 7) / 2));
                return true;
            };

            for (var i = 8; i <= 20; i += 2)
            {
                InterfaceInstance.AttachClickHandler(i, clickHandler);
            }

            InterfaceInstance.AttachClickHandler(21, (componentID, clickType, extra1, extra2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                Owner.Widgets.CloseWidget(InterfaceInstance);
                return true;
            });

            OnComponentClick clickHandler2 = (componentID, clickType, extra1, extra2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                _gameMediator.Publish(new ProfileSetIntAction(ProfileConstants.SummoningSettingsOrbOptionId, 7));
                return true;
            };
            InterfaceInstance.AttachClickHandler(26, clickHandler2);

            Owner.Configurations.SendGlobalCs2Int(168, 8); // set active tab.

            Refresh();
        }


        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() => Owner.Configurations.SendGlobalCs2Int(168, 4);

        public void Refresh()
        {
            var optionId = Owner.Profile.GetValue<int>(ProfileConstants.SummoningSettingsOrbOptionId);
            Owner.Configurations.SendStandardConfiguration(1493, optionId);
            Owner.Configurations.SendStandardConfiguration(1494, optionId); // refresh button position
        }
    }
}