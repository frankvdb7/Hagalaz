using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.Bank;

namespace Hagalaz.Game.Scripts.Widgets.EquipmentTab
{
    /// <summary>
    ///     The equipment stats screen where you can see the the total equipment bonuses.
    /// </summary>
    public class EquipmentWindow : WidgetScript
    {
        /// <summary>
        ///     Contains inventory interface instance.
        /// </summary>
        private IWidget _inventoryInterface;

        /// <summary>
        ///     Contains equipment changes unEquipHandler.
        /// </summary>
        private EventHappened _handler;

        public EquipmentWindow(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Happens when interface is opened.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetVisible(87, !Owner.HasState(StateType.Banking)); // disable close button when banking
            var script = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
            if (!Owner.Widgets.OpenInventoryOverlay(670, 1, script))
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            _inventoryInterface = Owner.Widgets.GetOpenWidget(670);
            if (_inventoryInterface == null)
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            Owner.Configurations.SendBitConfiguration(4894, Owner.HasState(StateType.Banking) ? 1 : 0);

            InterfaceInstance.SetOptions(9, 0, 14, 1026);
            _inventoryInterface.SetOptions(0, 0, 27, 1026);

            InterfaceInstance.AttachClickHandler(9, (component, type, itemID, slot) =>
            {
                if (slot < 0 || slot >= Owner.Equipment.Capacity)
                {
                    return false;
                }

                var item = Owner.Equipment[(EquipmentSlot)slot];
                if (item == null || item.Id != itemID)
                {
                    return false;
                }

                if (type == ComponentClickType.LeftClick)
                {
                    if (!item.EquipmentScript.UnEquipItem(item, Owner))
                    {
                        Owner.SendChatMessage("You can't unequip that.");
                    }

                    return true;
                }

                if (type == ComponentClickType.Option10Click)
                {
                    Owner.SendChatMessage(item.ItemScript.GetExamine(item));
                    return true;
                }

                return false;
            });
            _inventoryInterface.AttachClickHandler(0, (component, type, itemID, slot) =>
            {
                if (slot < 0 || slot >= Owner.Inventory.Capacity)
                {
                    return false;
                }

                var item = Owner.Inventory[slot];
                if (item == null || item.Id != itemID)
                {
                    return false;
                }

                if (type == ComponentClickType.LeftClick)
                {
                    if (item.EquipmentDefinition.Slot == EquipmentSlot.NoSlot || !item.EquipmentScript.EquipItem(item, Owner))
                    {
                        Owner.SendChatMessage("You can't wear that.");
                    }

                    return true;
                }

                if (type == ComponentClickType.Option10Click)
                {
                    Owner.SendChatMessage(item.ItemScript.GetExamine(item));
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(46, (component, type, itemID, slot) =>
            {
                if (type == ComponentClickType.LeftClick)
                {
                    Owner.RemoveState(StateType.Banking);
                    var bankScript = Owner.ServiceProvider.GetRequiredService<BankScreen>();
                    Owner.Widgets.OpenWidget(667, 0, bankScript, false);
                    return true;
                }

                return false;
            });

            _handler = Owner.RegisterEventHandler(new EventHappened<EquipmentChangedEvent>(e =>
            {
                RefreshBonuses();
                return false;
            }));
            RefreshBonuses();
        }

        /// <summary>
        ///     Happens when interface is closed.
        /// </summary>
        public override void OnClose()
        {
            if (_handler != null)
            {
                Owner.UnregisterEventHandler<EquipmentChangedEvent>(_handler);
            }

            if (_inventoryInterface != null)
            {
                Owner.Widgets.CloseWidget(_inventoryInterface);
            }
        }

        /// <summary>
        ///     Refreshe's bonuses in interface.
        /// </summary>
        public void RefreshBonuses()
        {
            var attackStab = Owner.Statistics.Bonuses.GetBonus(BonusType.AttackStab);
            var attackSlash = Owner.Statistics.Bonuses.GetBonus(BonusType.AttackSlash);
            var attackCrush = Owner.Statistics.Bonuses.GetBonus(BonusType.AttackCrush);
            var attackMagic = Owner.Statistics.Bonuses.GetBonus(BonusType.AttackMagic);
            var attackRanged = Owner.Statistics.Bonuses.GetBonus(BonusType.AttackRanged);
            var defenceStab = Owner.Statistics.Bonuses.GetBonus(BonusType.DefenceStab);
            var defenceSlash = Owner.Statistics.Bonuses.GetBonus(BonusType.DefenceSlash);
            var defenceCrush = Owner.Statistics.Bonuses.GetBonus(BonusType.DefenceCrush);
            var defenceMagic = Owner.Statistics.Bonuses.GetBonus(BonusType.DefenceMagic);
            var defenceRanged = Owner.Statistics.Bonuses.GetBonus(BonusType.DefenceRanged);
            var defenceSummoning = Owner.Statistics.Bonuses.GetBonus(BonusType.DefenceSummoning);
            var absorbMelee = Owner.Statistics.Bonuses.GetBonus(BonusType.AbsorbMelee);
            var absorbMagic = Owner.Statistics.Bonuses.GetBonus(BonusType.AbsorbMagic);
            var absorbRange = Owner.Statistics.Bonuses.GetBonus(BonusType.AbsorbRange);
            var strength = Owner.Statistics.Bonuses.GetBonus(BonusType.Strength);
            var rangedStrength = Owner.Statistics.Bonuses.GetBonus(BonusType.RangedStrength);
            var prayer = Owner.Statistics.Bonuses.GetBonus(BonusType.Prayer);
            var magicDamage = Owner.Statistics.Bonuses.GetBonus(BonusType.MagicDamage);

            // attack bonuses
            InterfaceInstance.DrawString(28, "Stab: " + (attackStab > 0 ? "+" : "") + attackStab);
            InterfaceInstance.DrawString(29, "Slash: " + (attackSlash > 0 ? "+" : "") + attackSlash);
            InterfaceInstance.DrawString(30, "Crush: " + (attackCrush > 0 ? "+" : "") + attackCrush);
            InterfaceInstance.DrawString(31, "Magic: " + (attackMagic > 0 ? "+" : "") + attackMagic);
            InterfaceInstance.DrawString(32, "Ranged: " + (attackRanged > 0 ? "+" : "") + attackRanged);

            // defence bonuses
            InterfaceInstance.DrawString(33, "Stab: " + (defenceStab > 0 ? "+" : "") + defenceStab);
            InterfaceInstance.DrawString(34, "Slash: " + (defenceSlash > 0 ? "+" : "") + defenceSlash);
            InterfaceInstance.DrawString(35, "Crush: " + (defenceCrush > 0 ? "+" : "") + defenceCrush);
            InterfaceInstance.DrawString(36, "Magic: " + (defenceMagic > 0 ? "+" : "") + defenceMagic);
            InterfaceInstance.DrawString(37, "Ranged: " + (defenceRanged > 0 ? "+" : "") + defenceRanged);
            InterfaceInstance.DrawString(38, "Summoning: " + (defenceSummoning > 0 ? "+" : "") + defenceSummoning);
            InterfaceInstance.DrawString(39, "Absorb Melee: " + (absorbMelee > 0 ? "+" : "") + absorbMelee + "%");
            InterfaceInstance.DrawString(40, "Absorb Magic: " + (absorbMagic > 0 ? "+" : "") + absorbMagic + "%");
            InterfaceInstance.DrawString(41, "Absorb Ranged: " + (absorbRange > 0 ? "+" : "") + absorbRange + "%");

            // other bonuses
            InterfaceInstance.DrawString(42, "Strength: " + (strength > 0 ? "+" : "") + strength);
            InterfaceInstance.DrawString(43, "Ranged Strength: " + (rangedStrength > 0 ? "+" : "") + rangedStrength);
            InterfaceInstance.DrawString(44, "Prayer: " + (prayer > 0 ? "+" : "") + prayer);
            InterfaceInstance.DrawString(45, "Magic Damage: " + (magicDamage > 0 ? "+" : "") + magicDamage + "%");
        }
    }
}