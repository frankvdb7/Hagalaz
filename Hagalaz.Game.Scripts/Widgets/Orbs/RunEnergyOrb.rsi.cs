using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Scripts.Widgets.Orbs;

namespace Hagalaz.Game.Scripts.Widgets.Orbs
{
    /// <summary>
    ///     Represents run energy orb.
    /// </summary>
    public class RunEnergyOrb : WidgetScript
    {
        /// <summary>
        ///     The rest information.
        ///     Dimension 0 = kind of animation.
        ///     Diminsion 1 = animation. - 0 = start rest anim, 1 = render anim, 2 = end rest anim.
        /// </summary>
        private static readonly int[,] _restInfo = {{5713, 1549, 5748}, {11786, 1550, 11788}, {5713, 1551, 2921}};

        private readonly IScopedGameMediator _gameMediator;
        private IGameConnectHandle _gameConnectHandle = default!;

        public RunEnergyOrb(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator) : base(characterContextAccessor) => _gameMediator = gameMediator;

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            _gameConnectHandle = _gameMediator.ConnectHandler<ProfileValueChanged<bool>>((context) =>
            {
                if (context.Message.Key == ProfileConstants.RunSettingsToggled)
                {
                    RefreshOrbState();
                }
            });

            // toggle running
            InterfaceInstance.AttachClickHandler(4, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.RunSettingsToggled));
                }
                else if (clickType == ComponentClickType.Option2Click)
                {
                    if (Owner.HasState<RestingState>())
                    {
                        Owner.RemoveState<RestingState>();
                    }
                    else
                    {
                        StartResting();
                    }
                }

                return false;
            });

            Refresh();
        }

        /// <summary>
        ///     Starts the resting.
        /// </summary>
        public void StartResting()
        {
            Owner.Movement.ClearQueue();
            var index = RandomStatic.Generator.Next(0, _restInfo.GetLength(0));
            Owner.QueueAnimation(Animation.Create(_restInfo[index, 0]));
            Owner.Appearance.RenderId = _restInfo[index, 1];
            Owner.AddState(new RunEnergyOrbRestingState
            {
                OnRemovedCallback = () =>
                {
                    Owner.QueueAnimation(Animation.Create(_restInfo[index, 2]));
                    Owner.Appearance.ResetRenderID();
                    RefreshOrbState();
                }
            });
            RefreshOrbState();
        }

        /// <summary>
        ///     Refreshes this instance.
        /// </summary>
        public void Refresh() 
        { 
            Owner.Statistics.RefreshRunEnergy();
            RefreshOrbState();
        }

        public void RefreshOrbState() => Owner.Configurations.SendStandardConfiguration(173, Owner.HasState<ListeningToMusicianState>() ? 4 : Owner.HasState<RestingState>() ? 3 : (Owner.Profile.GetValue<bool>(ProfileConstants.RunSettingsToggled) ? 1 : 0));

/// <summary>
///     Happens when interface is closed for character.
/// </summary>
public override void OnClose() => _gameConnectHandle?.Disconnect();
    }
}