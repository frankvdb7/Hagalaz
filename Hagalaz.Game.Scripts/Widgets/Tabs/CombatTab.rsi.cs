using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    ///     Represents the combat tab.
    /// </summary>
    public class CombatTab : WidgetScript
    {
        private readonly IScopedGameMediator _gameMediator;
        private IGameConnectHandle _attackStyleChanged = default!;
        private IGameConnectHandle _autoRetaliateChanged = default!;

        public CombatTab(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator) : base(characterContextAccessor)
        {
            _gameMediator = gameMediator;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetOptions(7, 65535, 65535, 2);
            InterfaceInstance.SetOptions(8, 65535, 65535, 2);
            InterfaceInstance.SetOptions(9, 65535, 65535, 2);
            InterfaceInstance.SetOptions(10, 65535, 65535, 2);

            _attackStyleChanged = _gameMediator.ConnectHandler<ProfileValueChanged<int>>(async (context) =>
            {
                if (context.Message.Key == ProfileConstants.CombatSettingsAttackStyleOptionId)
                {
                    RefreshAttackStyleOptionId();
                }
            });

            _autoRetaliateChanged = _gameMediator.ConnectHandler<ProfileValueChanged<bool>>(async (context) =>
            {
                if (context.Message.Key == ProfileConstants.CombatSettingsAutoRetaliate)
                {
                    RefreshAutoRetaliate();
                }
                if (context.Message.Key == ProfileConstants.CombatSettingsSpecialAttack)
                {
                    RefreshSpecialAttack();
                }
            });

            OnComponentClick styleClickHandler = (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }
                var optionId = componentID - 7;
                if (Owner.Combat.GetAttackBonusType() == AttackBonus.None) 
                { 
                    componentID = 0; 
                }
                Owner.Magic.ClearAutoCastingSpell(false);
                _gameMediator.Publish(new ProfileSetIntAction(ProfileConstants.CombatSettingsAttackStyleOptionId, componentID - 7));
                return true;
            };
            OnComponentClick autoRetaliateHandler = (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.CombatSettingsAutoRetaliate));
                return true;
            };
            OnComponentClick specialBarHandler = (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!new SpecialAllowEvent(Owner).Send())
                {
                    return false;
                }

                if (Owner.Profile.GetValue<bool>(ProfileConstants.CombatSettingsSpecialAttack)) 
                {
                    _gameMediator.Publish(new ProfileSetBoolAction(ProfileConstants.CombatSettingsSpecialAttack, false));
                }
                else
                {
                    var weapon = Owner.Equipment[EquipmentSlot.Weapon];
                    if (weapon == null)
                    {
                        return false;
                    }

                    if (weapon.EquipmentScript.SpecialBarEnableClicked(weapon, Owner))
                    {
                        _gameMediator.Publish(new ProfileSetBoolAction(ProfileConstants.CombatSettingsSpecialAttack, true));
                    }
                }
                return true;
            };

            InterfaceInstance.AttachClickHandler(4, specialBarHandler);
            InterfaceInstance.AttachClickHandler(7, styleClickHandler);
            InterfaceInstance.AttachClickHandler(8, styleClickHandler);
            InterfaceInstance.AttachClickHandler(9, styleClickHandler);
            InterfaceInstance.AttachClickHandler(10, styleClickHandler);
            InterfaceInstance.AttachClickHandler(11, autoRetaliateHandler);

            Owner.Statistics.RefreshSpecialEnergy();

            Refresh();
        }

        public void Refresh()
        {
            RefreshAttackStyleOptionId();
            RefreshAutoRetaliate();
            RefreshSpecialAttack();
        }

        public void RefreshAutoRetaliate()
        {
            Owner.Configurations.SendStandardConfiguration(172, Owner.Profile.GetValue<bool>(ProfileConstants.CombatSettingsAutoRetaliate) ? 0 : 1);
        }

        public void RefreshAttackStyleOptionId()
        {
            Owner.Configurations.SendStandardConfiguration(43, Owner.Profile.GetValue<int>(ProfileConstants.CombatSettingsAttackStyleOptionId));
        }

        public void RefreshSpecialAttack()
        {
            Owner.Configurations.SendStandardConfiguration(301, Owner.Profile.GetValue<bool>(ProfileConstants.CombatSettingsSpecialAttack) ? 1 : 0);
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            _attackStyleChanged?.Disconnect();
            _autoRetaliateChanged?.Disconnect();
        }
    }
}