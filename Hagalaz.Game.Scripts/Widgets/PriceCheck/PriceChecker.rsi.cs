using System;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Items;

namespace Hagalaz.Game.Scripts.Widgets.PriceCheck
{
    /// <summary>
    ///     Represents price checker interface.
    /// </summary>
    public class PriceChecker : WidgetScript
    {
        /// <summary>
        ///     Contains inventory interface.
        /// </summary>
        private IWidget? _inventoryInterface;

        /// <summary>
        ///     Contains the selected items while the price checker is open.
        /// </summary>
        private ItemContainer? _priceCheckItems;

        /// <summary>
        ///     Handler for adding X amount of items from the owner's inventory to the price check interface.
        /// </summary>
        private OnIntInput? _inputHandler;

        private EventHappened? _inventoryChangeHandler;

        public PriceChecker(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            // open inventory overlay.
            var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
            if (!Owner.Widgets.OpenInventoryOverlay(207, 1, defaultScript))
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            var inventoryInterface = Owner.Widgets.GetOpenWidget(207);
            if (inventoryInterface == null)
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }
            _inventoryInterface = inventoryInterface;

            // set options
            Owner.Configurations.SendCs2Script(150, [(207 << 16) | 0, 93, 4, 7, 0, -1, "Insert", "Insert-5", "Insert-10", "Insert-All", "Insert-X", "", "", "", ""
            ]);
            inventoryInterface.SetOptions(0, 0, 27, 0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x400); // allow clicking of 5 right click options + auto examine option ( last )
            InterfaceInstance.SetOptions(15, 0, 27, 0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x400);
            Owner.Configurations.SendGlobalCs2Int(729, 0);

            // price check interface & clear interface items (from previous price checks).
            _priceCheckItems = new ItemContainer(StorageType.AlwaysStack, Owner.Inventory.Items.Capacity);

            _inventoryChangeHandler = Owner.RegisterEventHandler<InventoryChangedEvent>(OnInventoryChanged);

            RefreshPriceChecker(fullRefresh: true);
            RefreshProjectedInventory();


            // Component attachment for inventory (for ability to add items to price check interface).
            inventoryInterface.AttachClickHandler(0, (componentID, clickType, itemID, slot) =>
            {
                if (slot < 0 || slot >= Owner.Inventory.Items.Capacity)
                {
                    return false;
                }

                var item = Owner.Inventory.Items[slot];
                if (item == null || item.Id != itemID)
                {
                    return false;
                }

                if (!item.ItemScript.CanTradeItem(item, Owner))
                {
                    Owner.SendChatMessage("You can't add this item to the pricechecker.");
                    return false;
                }

                var amount = 0;
                if (clickType == ComponentClickType.LeftClick)
                {
                    amount = 1;
                }
                else if (clickType == ComponentClickType.Option2Click)
                {
                    amount = 5;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    amount = 10;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    amount = Owner.Inventory.Items.GetCount(item);
                }
                else if (clickType == ComponentClickType.Option5Click)
                {
                    _inputHandler = Owner.Widgets.IntInputHandler = value =>
                    {
                        _inputHandler = Owner.Widgets.IntInputHandler = null;
                        if (value > 0)
                        {
                            AddItemToPriceChecker(item, value);
                        }
                    };
                    Owner.Configurations.SendIntegerInput("Enter amount:");
                }
                else if (clickType == ComponentClickType.Option10Click)
                {
                    Owner.SendChatMessage(item.ItemScript.GetExamine(item));
                }

                if (amount > 0)
                {
                    AddItemToPriceChecker(item, amount);
                }

                return false;
            });

            // Component attachment for price checker (for ability to remove items to owner's inventory).
            InterfaceInstance.AttachClickHandler(15, (componentID, clickType, itemID, slot) =>
            {
                var items = _priceCheckItems;
                if (items == null)
                {
                    return false;
                }

                if (slot < 0 || slot >= items.Capacity)
                {
                    return false;
                }

                var item = items[slot];
                if (item == null || item.Id != itemID)
                {
                    return false;
                }

                var amount = 0;
                if (clickType == ComponentClickType.LeftClick)
                {
                    amount = 1;
                }
                else if (clickType == ComponentClickType.Option2Click)
                {
                    amount = 5;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    amount = 10;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    amount = items.GetCount(item);
                }
                else if (clickType == ComponentClickType.Option5Click)
                {
                    _inputHandler = Owner.Widgets.IntInputHandler = value =>
                    {
                        _inputHandler = Owner.Widgets.IntInputHandler = null;
                        if (value > 0)
                        {
                            RemoveSelection(item, value);
                        }
                    };
                    Owner.Configurations.SendIntegerInput("Enter amount to remove:");
                }
                else if (clickType == ComponentClickType.Option10Click)
                {
                    Owner.SendChatMessage(item.ItemScript.GetExamine(item));
                }

                if (amount > 0)
                {
                    RemoveSelection(item, amount);
                }

                return false;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_inventoryChangeHandler != null)
            {
                Owner.UnregisterEventHandler<InventoryChangedEvent>(_inventoryChangeHandler);
                _inventoryChangeHandler = null;
            }

            if (_inputHandler != null && Owner.Widgets.IntInputHandler == _inputHandler)
            {
                Owner.Widgets.IntInputHandler = null;
            }

            _inputHandler = null;
            if (_inventoryInterface?.IsOpened == true)
            {
                Owner.Widgets.CloseWidget(_inventoryInterface);
            }

            _inventoryInterface = null;
            _priceCheckItems = null;
            Owner.Configurations.SendItems(93, false, Owner.Inventory.Items);
        }

        /// <summary>
        ///     Adds an item from the owner's inventory to the price checker interface.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="amount">The item amount.</param>
        /// <returns>
        ///     Returns true if successfully added to the price checker interface; false otherwise.
        /// </returns>
        private bool AddItemToPriceChecker(IItem item, int amount)
        {
            var items = _priceCheckItems;
            if (items == null)
            {
                return false;
            }

            var available = Owner.Inventory.Items.GetCount(item) - items.GetCount(item);
            var count = Math.Min(amount, available);
            if (count <= 0 || !items.Add(item.Clone(count)))
            {
                return false;
            }

            RefreshPriceChecker();
            RefreshProjectedInventory();
            return true;
        }

        /// <summary>
        ///     Removes an item from the temporary price checker selection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="amount">The item amount.</param>
        /// <returns>
        ///     Returns true if successfully removed; false otherwise.
        /// </returns>
        private bool RemoveSelection(IItem item, int amount)
        {
            var items = _priceCheckItems;
            if (items == null)
            {
                return false;
            }

            var count = Math.Min(amount, items.GetCount(item));
            if (count <= 0 || items.Remove(item.Clone(count)) != count)
            {
                return false;
            }

            RefreshPriceChecker();
            RefreshProjectedInventory();
            return true;
        }

        private bool ReconcileSelections()
        {
            var items = _priceCheckItems;
            if (items == null)
            {
                return false;
            }

            var changed = false;
            for (var slot = 0; slot < items.Capacity; slot++)
            {
                if (items[slot] is not { } selected)
                {
                    continue;
                }

                var actual = Owner.Inventory.Items.GetCount(selected);
                if (selected.Count > actual)
                {
                    items.Remove(selected.Clone(selected.Count - actual), slot);
                    changed = true;
                }
            }

            return changed;
        }

        private bool OnInventoryChanged(InventoryChangedEvent _)
        {
            if (_priceCheckItems == null)
            {
                return false;
            }

            if (ReconcileSelections())
            {
                RefreshPriceChecker();
            }

            RefreshProjectedInventory();
            return false;
        }

        private void RefreshProjectedInventory()
        {
            var items = _priceCheckItems;
            if (items == null)
            {
                return;
            }

            var projected = new ItemContainer(StorageType.Normal, Owner.Inventory.Items.Capacity);
            for (var slot = 0; slot < Owner.Inventory.Items.Capacity; slot++)
            {
                if (Owner.Inventory.Items[slot] is { } item)
                {
                    projected.Replace(slot, item.Clone());
                }
            }

            for (var slot = 0; slot < items.Capacity; slot++)
            {
                if (items[slot] is { } selected)
                {
                    projected.TryRemoveExact(selected);
                }
            }

            Owner.Configurations.SendItems(93, false, projected);
        }

        private void RefreshPriceChecker(bool fullRefresh = false)
        {
            var items = _priceCheckItems;
            if (items == null) return;

            var slots = items.ChangeTracker.GetSlots();
            if (!fullRefresh && slots is { Count: 0 }) return;
            if (fullRefresh) slots = null;

            Owner.Configurations.SendItems(90, false, items, slots);
            Owner.Configurations.SendGlobalCs2Int(728, (int)ItemContainerTradeValue.Calculate(items));

            if (slots == null)
            {
                for (var slot = 0; slot < items.Capacity; slot++)
                {
                    Owner.Configurations.SendGlobalCs2Int(700 + slot, items[slot]?.ItemDefinition.TradeValue ?? 0);
                }
            }
            else
            {
                foreach (var slot in slots)
                {
                    Owner.Configurations.SendGlobalCs2Int(700 + slot, items[slot]?.ItemDefinition.TradeValue ?? 0);
                }
            }
        }
    }
}
