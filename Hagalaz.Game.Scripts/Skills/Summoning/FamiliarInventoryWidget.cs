using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Summoning
{
    /// <summary>
    /// </summary>
    public class FamiliarInventoryWidget : WidgetScript
    {
        /// <summary>
        ///     Contains inventory interface.
        /// </summary>
        private IWidget? _inventoryInterface;

        /// <summary>
        ///     Contains bank change unEquipHandler.
        /// </summary>
        private EventHappened? _familiarInventoryChangeHandler;

        /// <summary>
        ///     Contains inventory change unEquipHandler.
        /// </summary>
        private EventHappened? _inventoryChangeHandler;

        /// <summary>
        ///     Contains inventory X unEquipHandler.
        /// </summary>
        private OnIntInput? _inventoryXHandler;

        public FamiliarInventoryWidget(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
            if (!Owner.Widgets.OpenInventoryOverlay(665, 1, defaultScript))
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            _inventoryInterface = Owner.Widgets.GetOpenWidget(665);
            if (_inventoryInterface == null)
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            Setup();
        }

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        public void Setup()
        {
            InterfaceInstance.SetOptions(27, 0, 30, 1150);
            _inventoryInterface!.SetOptions(0, 0, 28, 1150);

            Owner.Configurations.SendCs2Script(150,
            [
                (671 << 16) | 27, 30, 6, 5, -1, "Withdraw<col=FF9040>", "Withdraw-5<col=FF9040>", "Withdraw-10<col=FF9040>", "Withdraw-X<col=FF9040>",
                "Withdraw-All<col=FF9040>"
            ]);
            Owner.Configurations.SendCs2Script(150,
            [
                (665 << 16) | 0, 90, 4, 7, 0, "Store<col=FF9040>", "Store-5<col=FF9040>", "Store-10<col=FF9040>", "Store-X<col=FF9040>", "Store-All<col=FF9040>"
            ]);

            _familiarInventoryChangeHandler = Owner.RegisterEventHandler(new EventHappened<FamiliarInventoryChangedEvent>(e =>
            {
                RefreshFamiliarInventory(e.ChangedSlots);
                return false;
            }));
            _inventoryChangeHandler = Owner.RegisterEventHandler(new EventHappened<InventoryChangedEvent>(e =>
            {
                RefreshInventory(e.ChangedSlots);
                return false;
            }));
            _inventoryInterface.AttachClickHandler(0,
                (componentID, type, itemID, slot) =>
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

                    if (Owner.FamiliarScript is not BobFamiliarScriptBase bob)
                    {
                        return false;
                    }

                    var amount = 0;
                    if (type == ComponentClickType.LeftClick)
                    {
                        amount = 1;
                    }
                    else if (type == ComponentClickType.Option2Click)
                    {
                        amount = 5;
                    }
                    else if (type == ComponentClickType.Option3Click)
                    {
                        amount = 10;
                    }
                    else if (type == ComponentClickType.Option5Click)
                    {
                        amount = Owner.Inventory.GetCount(item);
                    }
                    else if (type == ComponentClickType.Option4Click)
                    {
                        _inventoryXHandler = Owner.Widgets.IntInputHandler = value =>
                        {
                            _inventoryXHandler = Owner.Widgets.IntInputHandler = null;
                            if (value <= 0)
                            {
                                Owner.SendChatMessage("Value can't be negative.");
                            }
                            else
                            {
                                bob.Inventory.DepositFromInventory(item, value);
                            }
                        };
                        Owner.Configurations.SendIntegerInput("Please enter the amount to deposit:");
                        return true;
                    }
                    else if (type == ComponentClickType.Option10Click)
                    {
                        Owner.SendChatMessage(item.ItemScript.GetExamine(item));
                    }
                    else
                    {
                        return false;
                    }

                    if (amount > 0)
                    {
                        bob.Inventory.DepositFromInventory(item, amount);
                    }

                    return true;
                });

            InterfaceInstance.AttachClickHandler(27,
                (componentID, type, itemID, slot) =>
                {
                    if (Owner.FamiliarScript is not BobFamiliarScriptBase bob)
                    {
                        return false;
                    }

                    if (slot < 0 || slot >= bob.Inventory.Capacity)
                    {
                        return false;
                    }

                    var item = bob.Inventory[slot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    var amount = 0;
                    if (type == ComponentClickType.LeftClick)
                    {
                        amount = 1;
                    }
                    else if (type == ComponentClickType.Option2Click)
                    {
                        amount = 5;
                    }
                    else if (type == ComponentClickType.Option3Click)
                    {
                        amount = 10;
                    }
                    else if (type == ComponentClickType.Option5Click)
                    {
                        amount = bob.Inventory.GetCount(item);
                    }
                    else if (type == ComponentClickType.Option4Click)
                    {
                        _inventoryXHandler = Owner.Widgets.IntInputHandler = value =>
                        {
                            _inventoryXHandler = Owner.Widgets.IntInputHandler = null;
                            if (value <= 0)
                            {
                                Owner.SendChatMessage("Value can't be negative.");
                            }

                            bob.Inventory.WithdrawFromFamiliarInventory(item, value);
                        };
                        Owner.Configurations.SendIntegerInput("Please enter the amount to withdraw:");
                        return true;
                    }
                    else if (type == ComponentClickType.Option10Click)
                    {
                        Owner.SendChatMessage(item.ItemScript.GetExamine(item));
                    }
                    else
                    {
                        return false;
                    }

                    if (amount > 0)
                    {
                        bob.Inventory.WithdrawFromFamiliarInventory(item, amount);
                    }

                    return true;
                });

            InterfaceInstance.AttachClickHandler(29,
                (componentID, type, itemID, slot) =>
                {
                    if (Owner.FamiliarScript is not BobFamiliarScriptBase bob)
                    {
                        return false;
                    }

                    Owner.Inventory.AddAndRemoveFrom(bob.Inventory);
                    return true;
                });

            RefreshFamiliarInventory(null);
            RefreshInventory(null);
        }

        /// <summary>
        ///     Refreshe's bank.
        /// </summary>
        public void RefreshFamiliarInventory(HashSet<int>? changedSlots = null)
        {
            if (Owner.FamiliarScript is not BobFamiliarScriptBase bob)
            {
                return;
            }

            Owner.Configurations.SendItems(30, false, bob.Inventory, changedSlots);
        }

        /// <summary>
        ///     Refreshe's inventory.
        /// </summary>
        /// <param name="changedSlots"></param>
        public void RefreshInventory(HashSet<int>? changedSlots = null)
        {
            Owner.Configurations.SendItems(90, false, Owner.Inventory, changedSlots);
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_inventoryInterface != null)
            {
                Owner.Widgets.CloseWidget(_inventoryInterface);
            }

            if (_familiarInventoryChangeHandler != null)
            {
                Owner.UnregisterEventHandler<FamiliarInventoryChangedEvent>(_familiarInventoryChangeHandler);
            }

            if (_inventoryChangeHandler != null)
            {
                Owner.UnregisterEventHandler<InventoryChangedEvent>(_inventoryChangeHandler);
            }

            if (_inventoryXHandler != null)
            {
                if (Owner.Widgets.IntInputHandler == _inventoryXHandler)
                {
                    Owner.Widgets.IntInputHandler = null;
                }

                _inventoryXHandler = null;
            }
        }
    }
}