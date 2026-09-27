using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.PriceCheck
{
    /// <summary>
    ///     Represents price checker interface.
    /// </summary>
    public class PriceChecker : WidgetScript
    {
        /// <summary>
        ///     Price checker interface container.
        /// </summary>
        private class PriceCheckerInterfaceContainer : BaseItemContainer
        {
            /// <summary>
            ///     Contains owner of this class.
            /// </summary>
            private readonly ICharacter _owner;

            /// <summary>
            ///     Construct's new instance.
            /// </summary>
            /// <param name="owner"></param>
            public PriceCheckerInterfaceContainer(ICharacter owner) : base(StorageType.AlwaysStack, owner.Inventory.Capacity) => _owner = owner;

            /// <summary>
            ///     Happens when container is updated.
            /// </summary>
            /// <param name="slots"></param>
            public override void OnUpdate(HashSet<int>? slots = null)
            {
                _owner.Configurations.SendItems(90, false, this, slots);
                RefreshPrices(slots);
            }

            /// <summary>
            ///     Calculate's total value of this container.
            /// </summary>
            /// <returns></returns>
            public long CalculateTotalValue()
            {
                long total = 0;
                for (var slot = 0; slot < Capacity; slot++)
                {
                    var item = this[slot];
                    if (item == null)
                    {
                        continue;
                    }

                    if ((ulong)total + (ulong)item.ItemDefinition.TradeValue * (ulong)item.Count > int.MaxValue)
                    {
                        return -1;
                    }

                    total += item.ItemDefinition.TradeValue * item.Count;
                }

                return total;
            }


            /// <summary>
            ///     Refreshe's prices.
            /// </summary>
            /// <param name="slots"></param>
            public void RefreshPrices(HashSet<int>? slots = null)
            {
                _owner.Configurations.SendGlobalCs2Int(728, (int)CalculateTotalValue());

                if (slots == null)
                {
                    for (var i = 0; i < Capacity; i++)
                    {
                        if (this[i] != null)
                        {
                            _owner.Configurations.SendGlobalCs2Int(700 + i, this[i]!.ItemDefinition.TradeValue);
                        }
                        else
                        {
                            _owner.Configurations.SendGlobalCs2Int(700 + 1, 0);
                        }
                    }
                }
                else
                {
                    foreach (var i in slots)
                    {
                        if (this[i] != null)
                        {
                            _owner.Configurations.SendGlobalCs2Int((short)(700 + i), this[i]!.ItemDefinition.TradeValue);
                        }
                        else
                        {
                            _owner.Configurations.SendGlobalCs2Int(700 + 1, 0);
                        }
                    }
                }
            }
        }

        private sealed class ProjectedInventoryContainer : BaseItemContainer
        {
            public ProjectedInventoryContainer(int capacity) : base(StorageType.Normal, capacity) { }

            public bool RemoveExact(IItem item, int count)
            {
                lock (ContainerMutationLock)
                {
                    return TryRemoveExactCore(item, count, -1, out _);
                }
            }

            public override void OnUpdate(HashSet<int>? slots = null) { }
        }

        /// <summary>
        ///     Contains inventory interface.
        /// </summary>
        private IWidget? _inventoryInterface;

        /// <summary>
        ///     Contains price check interface container.
        /// </summary>
        private PriceCheckerInterfaceContainer? _priceCheckInterface;

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
            var priceCheckInterface = new PriceCheckerInterfaceContainer(Owner);
            _priceCheckInterface = priceCheckInterface;

            _inventoryChangeHandler = Owner.RegisterEventHandler<InventoryChangedEvent>(OnInventoryChanged);

            priceCheckInterface.OnUpdate();
            RefreshProjectedInventory();


            // Component attachment for inventory (for ability to add items to price check interface).
            inventoryInterface.AttachClickHandler(0, (componentID, clickType, itemID, slot) =>
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
                    amount = Owner.Inventory.GetCount(item);
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
                var priceCheckInterface = _priceCheckInterface;
                if (priceCheckInterface == null)
                {
                    return false;
                }

                if (slot < 0 || slot >= priceCheckInterface.Capacity)
                {
                    return false;
                }

                var item = priceCheckInterface[slot];
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
                    amount = priceCheckInterface.GetCount(item);
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
            _priceCheckInterface = null;
            Owner.Configurations.SendItems(93, false, Owner.Inventory);
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
            var priceCheckInterface = _priceCheckInterface;
            if (priceCheckInterface == null)
            {
                return false;
            }

            var available = Owner.Inventory.GetCount(item) - priceCheckInterface.GetCount(item);
            var count = Math.Min(amount, available);
            if (count <= 0 || !priceCheckInterface.Add(item.Clone(count)))
            {
                return false;
            }

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
            var priceCheckInterface = _priceCheckInterface;
            if (priceCheckInterface == null)
            {
                return false;
            }

            var count = Math.Min(amount, priceCheckInterface.GetCount(item));
            if (count <= 0 || priceCheckInterface.Remove(item.Clone(count)) != count)
            {
                return false;
            }

            RefreshProjectedInventory();
            return true;
        }

        private bool ReconcileSelections()
        {
            var priceCheckInterface = _priceCheckInterface;
            if (priceCheckInterface == null)
            {
                return false;
            }

            var changed = false;
            for (var slot = 0; slot < priceCheckInterface.Capacity; slot++)
            {
                if (priceCheckInterface[slot] is not { } selected)
                {
                    continue;
                }

                var actual = Owner.Inventory.GetCount(selected);
                if (selected.Count > actual)
                {
                    priceCheckInterface.Remove(selected.Clone(selected.Count - actual), slot, update: false);
                    changed = true;
                }
            }

            return changed;
        }

        private bool OnInventoryChanged(InventoryChangedEvent _)
        {
            var priceCheckInterface = _priceCheckInterface;
            if (priceCheckInterface == null)
            {
                return false;
            }

            if (ReconcileSelections())
            {
                priceCheckInterface.OnUpdate();
            }

            RefreshProjectedInventory();
            return false;
        }

        private void RefreshProjectedInventory()
        {
            var priceCheckInterface = _priceCheckInterface;
            if (priceCheckInterface == null)
            {
                return;
            }

            var projected = new ProjectedInventoryContainer(Owner.Inventory.Capacity);
            for (var slot = 0; slot < Owner.Inventory.Capacity; slot++)
            {
                if (Owner.Inventory[slot] is { } item)
                {
                    projected.Replace(slot, item.Clone());
                }
            }

            for (var slot = 0; slot < priceCheckInterface.Capacity; slot++)
            {
                if (priceCheckInterface[slot] is { } selected)
                {
                    projected.RemoveExact(selected, selected.Count);
                }
            }

            Owner.Configurations.SendItems(93, false, projected);
        }
    }
}
