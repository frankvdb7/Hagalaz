using System;
using System.Collections;
using System.Collections.Generic;
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
        ///     Price checker interface container.
        /// </summary>
        private class PriceCheckerInterfaceContainer : IItemContainer, IItemContainerStorageOwner
        {
            /// <summary>
            ///     Contains owner of this class.
            /// </summary>
            private readonly ICharacter _owner;
            private readonly ItemContainerStorage _storage;
            ItemContainerStorage IItemContainerStorageOwner.Storage => _storage;
            void IItemContainerStorageOwner.PublishChanges(HashSet<int>? slots) => OnUpdate(slots);
            public StorageType Type => _storage.Type;
            public int FreeSlots => _storage.FreeSlots;
            public int TakenSlots => _storage.TakenSlots;
            public IItem? this[int index] => _storage[index];
            public int Capacity => _storage.Capacity;
            public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Add(IItem item) { if (!_storage.TryAdd(item, out var slots)) return false; OnUpdate(slots); return true; }
            public bool Add(int slot, IItem item) { if (!_storage.TryAdd(slot, item, out var slots)) return false; OnUpdate(slots); return true; }
            public void AddAndRemoveFrom(IItemContainer source) => ItemContainerTransfer.AddAndRemoveFrom(this, source);
            public IItem? GetById(int id) => _storage.GetById(id);
            public int Remove(IItem item, int preferredSlot = -1, bool update = true)
            {
                var removed = _storage.Remove(item, preferredSlot, out var slots);
                if (removed > 0 && update) OnUpdate(slots);
                return removed;
            }
            public void Replace(int slot, IItem item) { _storage.Replace(slot, item); OnUpdate([slot]); }
            public void Swap(int fromSlot, int toSlot) { if (_storage.Swap(fromSlot, toSlot)) OnUpdate([fromSlot, toSlot]); }
            public void Move(int fromSlot, int toSlot) { if (_storage.Move(fromSlot, toSlot)) OnUpdate(null); }
            public bool AddRange(IEnumerable<IItem?> items) { if (!_storage.TryAddRange(items, out var slots)) return false; OnUpdate(slots); return true; }
            public bool Contains(int id, int count) => _storage.Contains(id, count);
            public bool Contains(int id) => _storage.Contains(id);
            public int GetCount(IItem item) => _storage.GetCount(item);
            public int GetCountById(int id) => _storage.GetCountById(id);
            public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);
            public void Sort() { _storage.Sort(); OnUpdate(null); }
            public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
            public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
            public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
            public void Clear(bool update) { if (_storage.Clear() && update) OnUpdate(null); }

            /// <summary>
            ///     Construct's new instance.
            /// </summary>
            /// <param name="owner"></param>
            public PriceCheckerInterfaceContainer(ICharacter owner) { _owner = owner; _storage = new ItemContainerStorage(StorageType.AlwaysStack, owner.Inventory.Capacity); }

            /// <summary>
            ///     Happens when container is updated.
            /// </summary>
            /// <param name="slots"></param>
            public void OnUpdate(HashSet<int>? slots = null)
            {
                _owner.Configurations.SendItems(90, false, this, slots);
                RefreshPrices(slots);
            }

            /// <summary>
            ///     Calculate's total value of this container.
            /// </summary>
            /// <returns></returns>
            public long CalculateTotalValue() => ItemContainerTradeValue.Calculate(this);


            /// <summary>
            ///     Refreshe's prices.
            /// </summary>
            /// <param name="slots"></param>
            public void RefreshPrices(HashSet<int>? slots = null)
            {
                _owner.Configurations.SendGlobalCs2Int(728, (int)CalculateTotalValue());

                if (slots == null)
                {
                    for (var i = 0; i < _storage.Capacity; i++)
                    {
                        if (_storage[i] != null)
                        {
                            _owner.Configurations.SendGlobalCs2Int(700 + i, _storage[i]!.ItemDefinition.TradeValue);
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
                        if (_storage[i] != null)
                        {
                            _owner.Configurations.SendGlobalCs2Int((short)(700 + i), _storage[i]!.ItemDefinition.TradeValue);
                        }
                        else
                        {
                            _owner.Configurations.SendGlobalCs2Int(700 + 1, 0);
                        }
                    }
                }
            }
        }

        private sealed class ProjectedInventoryContainer : IItemContainer, IItemContainerStorageOwner
        {
            private readonly ItemContainerStorage _storage;
            ItemContainerStorage IItemContainerStorageOwner.Storage => _storage;
            void IItemContainerStorageOwner.PublishChanges(HashSet<int>? slots) { }
            public ProjectedInventoryContainer(int capacity) => _storage = new ItemContainerStorage(StorageType.Normal, capacity);
            public IItem? this[int index] => _storage[index];
            public int Capacity => _storage.Capacity;
            public StorageType Type => _storage.Type;
            public int FreeSlots => _storage.FreeSlots;
            public int TakenSlots => _storage.TakenSlots;
            public IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Add(IItem item) => _storage.TryAdd(item, out _);
            public bool Add(int slot, IItem item) => _storage.TryAdd(slot, item, out _);
            public void AddAndRemoveFrom(IItemContainer source) => ItemContainerTransfer.AddAndRemoveFrom(this, source);
            public IItem? GetById(int id) => _storage.GetById(id);
            public int Remove(IItem item, int preferredSlot = -1, bool update = true) => _storage.Remove(item, preferredSlot, out _);
            public void Replace(int slot, IItem item) => _storage.Replace(slot, item);
            public void Swap(int fromSlot, int toSlot) => _storage.Swap(fromSlot, toSlot);
            public void Move(int fromSlot, int toSlot) => _storage.Move(fromSlot, toSlot);
            public bool AddRange(IEnumerable<IItem?> items) => _storage.TryAddRange(items, out _);
            public bool Contains(int id, int count) => _storage.Contains(id, count);
            public bool Contains(int id) => _storage.Contains(id);
            public int GetCount(IItem item) => _storage.GetCount(item);
            public int GetCountById(int id) => _storage.GetCountById(id);
            public int GetInstanceSlot(IItem instance) => _storage.GetInstanceSlot(instance);
            public void Sort() => _storage.Sort();
            public int GetSlotByItem(IItem item, bool ignoreCount = true) => _storage.GetSlotByItem(item, ignoreCount);
            public bool HasSpaceFor(IItem item) => _storage.HasSpaceFor(item);
            public bool HasSpaceForRange(IEnumerable<IItem?> items) => _storage.HasSpaceForRange(items);
            public void Clear(bool update) => _storage.Clear();
            public bool RemoveExact(IItem item, int count) => _storage.TryRemoveExact(item, count, -1, out _);
            public void OnUpdate(HashSet<int>? slots = null) { }
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

                if (slot < 0 || slot >= ((IItemContainer)priceCheckInterface).Capacity)
                {
                    return false;
                }

                var item = ((IItemContainer)priceCheckInterface)[slot];
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
            for (var slot = 0; slot < ((IItemContainer)priceCheckInterface).Capacity; slot++)
            {
                if (((IItemContainer)priceCheckInterface)[slot] is not { } selected)
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

            for (var slot = 0; slot < ((IItemContainer)priceCheckInterface).Capacity; slot++)
            {
                if (((IItemContainer)priceCheckInterface)[slot] is { } selected)
                {
                    projected.RemoveExact(selected, selected.Count);
                }
            }

            Owner.Configurations.SendItems(93, false, projected);
        }
    }
}
