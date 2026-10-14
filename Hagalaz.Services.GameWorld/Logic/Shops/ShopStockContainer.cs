using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;

namespace Hagalaz.Services.GameWorld.Logic.Shops
{
    /// <summary>
    /// Class ShopStockContainer
    /// </summary>
    public partial class ShopStockContainer : IShopStockContainer, IItemContainerCompletionOwner
    {
        /// <summary>
        /// Wether this shop container is a sample container.
        /// </summary>
        private readonly bool _sampleContainer;

        /// <summary>
        /// The shop that owns this container.
        /// </summary>
        private readonly IShop _shop;

        /// <summary>
        /// The item manager
        /// </summary>
        private readonly IItemService _itemRepository;

        private readonly IItemBuilder _itemBuilder;
        private readonly IEventManager _eventManager;
        private readonly ItemContainer _items;
        public IReadOnlyItemContainer Items => _items;

        private readonly Dictionary<int, int> _originalStockCounts;

        /// <summary>
        /// Constructs a container for character banks.
        /// </summary>
        /// <param name="shop">The owner.</param>
        /// <param name="itemRepository"></param>
        /// <param name="itemBuilder"></param>
        /// <param name="sampleContainer">if set to <c>true</c> [sample container].</param>
        /// <param name="type">The type of container.</param>
        /// <param name="capacity">The capacity of the container.</param>
        /// <param name="stock"></param>
        /// <param name="eventManager"></param>
        public ShopStockContainer(
            IShop shop, IItemService itemRepository, IItemBuilder itemBuilder, bool sampleContainer, StorageType type, int capacity,
            IList<IItem> stock, IEventManager eventManager)
        {
            _originalStockCounts = AggregateOriginalStock(stock);
            _items = new ItemContainer(type, stock, capacity, 0, this);
            _shop = shop;
            _sampleContainer = sampleContainer;
            _itemRepository = itemRepository;
            _itemBuilder = itemBuilder;
            _eventManager = eventManager;
        }

        private static Dictionary<int, int> AggregateOriginalStock(IEnumerable<IItem> stock)
        {
            ArgumentNullException.ThrowIfNull(stock);
            var counts = new Dictionary<int, int>();
            foreach (var item in stock)
            {
                ArgumentNullException.ThrowIfNull(item);
                counts.TryGetValue(item.Id, out var count);
                counts[item.Id] = checked(count + item.Count);
            }
            return counts;
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        void IItemContainerCompletionOwner.PublishCommittedChanges()
        {
            var slots = _items.ChangeTracker.GetSlots();
            if (slots is { Count: 0 }) return;
            if (_sampleContainer)
                _eventManager.SendEvent(new ShopSampleStockChangedEvent(_shop, slots));
            else
                _eventManager.SendEvent(new ShopStockChangedEvent(_shop, slots));
        }

        void IItemContainerCompletionOwner.DiscardPendingCompletion() { }

        public void SetItems(IItem[] items, bool update)
        {
            using var mutation = _items.BeginMutation();
            _items.ReplaceState(items);
            if (update) mutation.RecordChanges(null);
        }

        /// <summary>
        /// Sells specific item to the shop.
        /// </summary>
        /// <param name="viewer">The viewer.</param>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <returns>If depositing was successful.</returns>
        public bool SellFromInventory(ICharacter viewer, IItem item, int count)
        {
            var slot = viewer.Inventory.Items.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
            {
                return false;
            }

            var availableCount = viewer.Inventory.Items.GetCount(item);
            if (count > availableCount)
            {
                count = availableCount;
            }
            if (count <= 0)
            {
                return false;
            }

            var transformed = item.ItemDefinition.Noted && item.ItemDefinition.NoteId != -1;
            IItem sold;
            if (transformed)
            {
                sold = _itemBuilder.Create().WithId(item.ItemDefinition.NoteId).WithCount(count)
                    .WithExtraData(item.SerializeExtraData() ?? string.Empty).Build();
            }
            else
            {
                sold = item.Clone(count);
            }

            if (!sold.ItemScript.CanSellItem(sold, viewer) || !_shop.GeneralStore && _items.GetSlotByItem(sold) == -1)
            {
                viewer.SendChatMessage("You cannot sell this item.");
                return false;
            }

            if (!Items.HasSpaceFor(sold))
            {
                viewer.SendChatMessage("There is not enough space in the shop for this item.");
                return false;
            }

            var currencyCount = count * (long)_shop.GetSellValue(item);
            if (currencyCount > int.MaxValue)
            {
                viewer.SendChatMessage("The shop does not have enough money for this amount of items.");
                return false;
            }

            IItemTransactional[] participants = _shop.CurrencyId == 995
                ? [viewer.Inventory, _items, viewer.MoneyPouch]
                : [viewer.Inventory, _items];
            using var transaction = ItemContainerTransaction.Begin(participants);
            if (!viewer.Inventory.TryTransferItemTo(_items, item, count, slot,
                    destinationItem: transformed ? sold : null)) return false;
            var paid = _shop.CurrencyId == 995
                ? viewer.MoneyPouch.Add((int)currencyCount)
                : viewer.Inventory.AddItem(_itemBuilder.Create().WithId(_shop.CurrencyId).WithCount((int)currencyCount).Build());
            if (!paid) return false;
            transaction.Commit();
            return true;
        }

        /// <summary>
        /// Buys from shop.
        /// </summary>
        /// <param name="viewer">The viewer.</param>
        /// <param name="item">The item.</param>
        /// <param name="count">The count.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool BuyFromShop(ICharacter viewer, IItem item, int count)
        {
            var slot = Items.GetInstanceSlot(item);
            if (slot == -1 || count <= 0) return false;
            if (!item.ItemScript.CanBuyItem(item, viewer)) return false;
            var toRemove = item.Clone();
            if (toRemove.Count == 0)
            {
                viewer.SendChatMessage("There is no stock of that item at the moment.");
                return false;
            }

            if (toRemove.Count < count)
            {
                viewer.SendChatMessage("The shop has ran out of stock.");
                count = toRemove.Count;
            }

            toRemove.Count = count;
            var stack = toRemove.ItemDefinition.Stackable;
            var needSlots = 0;
            if (stack)
            {
                if (viewer.Inventory.Items.GetSlotByItem(toRemove) != -1)
                {
                    var total = viewer.Inventory.Items.GetCount(toRemove) + (long)count;
                    if (total > int.MaxValue)
                    {
                        return false;
                    }
                }
                else
                    needSlots = 1;
            }
            else
            {
                needSlots = count;
            }

            int freeSlots;
            if ((freeSlots = viewer.Inventory.Items.FreeSlots) < needSlots)
            {
                viewer.SendChatMessage("Not enough space in your inventory.");
                if (stack || freeSlots <= 0) // we can't do anything since decreasing item count won't decrease needSlots.
                {
                    return false;
                }

                count = freeSlots;
                toRemove.Count = count;
            }

            var cost = _sampleContainer ? 0 : _shop.GetBuyValue(item) * (long)count;
            if (cost > 0)
            {
                if (cost > int.MaxValue)
                {
                    viewer.SendChatMessage("The shop does not have enough money for this amount of items.");
                    return false;
                }

                var hasCurrency = _shop.CurrencyId == 995
                    ? viewer.MoneyPouch.HasCoins((int)cost)
                    : viewer.Inventory.Items.Contains(_shop.CurrencyId, (int)cost);
                if (!hasCurrency)
                {
                    viewer.SendChatMessage("You don't have enough " + _itemRepository.FindItemDefinitionById(_shop.CurrencyId).Name.ToLower() + "!");
                    return false;
                }
            }

            var originalStock = _originalStockCounts.ContainsKey(item.Id);
            IItemTransactional[] participants = _shop.CurrencyId == 995
                ? [_items, viewer.MoneyPouch]
                : [_items, viewer.Inventory];
            var insufficientCurrency = false;
            using (var transaction = ItemContainerTransaction.Begin(participants))
            {
                if (cost > 0)
                {
                    var paid = _shop.CurrencyId == 995
                        ? viewer.MoneyPouch.TryRemoveExact((int)cost)
                        : viewer.Inventory.TryRemoveItemExact(
                            _itemBuilder.Create().WithId(_shop.CurrencyId).WithCount((int)cost).Build());
                    insufficientCurrency = !paid;
                }
                if (!insufficientCurrency)
                {
                    if (!viewer.Inventory.TryTransferItemFrom(_items, item, count, slot)) return false;
                    if (!originalStock && Items[slot] == null) _items.Sort();
                    transaction.Commit();
                }
            }
            if (insufficientCurrency)
            {
                viewer.SendChatMessage("You don't have enough " + _itemRepository.FindItemDefinitionById(_shop.CurrencyId).Name.ToLower() + "!");
                return false;
            }
            _eventManager.SendEvent(new ShopItemBoughtEvent(viewer, _shop, toRemove));
            return true;
        }

        /// <summary>
        /// Normalizes the stock.
        /// </summary>
        public void NormalizeStock()
        {
            var changedSlots = new HashSet<int>();
            var positionsChanged = false;
            using (var mutation = _items.BeginMutation())
            {
                var currentItems = _items.SnapshotItems();
                var plannedItems = (IItem?[])currentItems.Clone();
                var countsById = new Dictionary<int, long>();
                foreach (var item in currentItems)
                {
                    if (item == null || item.Count <= 0) continue;
                    countsById.TryGetValue(item.Id, out var count);
                    countsById[item.Id] = count + item.Count;
                }

                var processedOriginalIds = new HashSet<int>();
                for (var slot = 0; slot < currentItems.Length; slot++)
                {
                    var item = currentItems[slot];
                    if (item == null) continue;

                    if (_originalStockCounts.TryGetValue(item.Id, out var baseline))
                    {
                        if (!processedOriginalIds.Add(item.Id)) continue;
                        countsById.TryGetValue(item.Id, out var currentCount);
                        if (currentCount < baseline)
                        {
                            var restoreSlot = Array.FindIndex(currentItems, candidate => candidate?.Id == item.Id);
                            if (restoreSlot >= 0)
                            {
                                plannedItems[restoreSlot] = CloneWithCount(currentItems[restoreSlot]!,
                                    checked(currentItems[restoreSlot]!.Count + 1));
                                changedSlots.Add(restoreSlot);
                            }
                        }
                        else if (currentCount > baseline)
                        {
                            var reduceSlot = Array.FindIndex(currentItems, candidate => candidate?.Id == item.Id && candidate.Count > 0);
                            if (reduceSlot >= 0)
                                DecreaseStockItem(currentItems, plannedItems, reduceSlot, changedSlots, ref positionsChanged);
                        }

                        continue;
                    }

                    if (item.Count > 0)
                        DecreaseStockItem(currentItems, plannedItems, slot, changedSlots, ref positionsChanged);
                }

                if (positionsChanged)
                {
                    var sortedItems = new IItem?[plannedItems.Length];
                    var write = 0;
                    for (var read = 0; read < plannedItems.Length; read++)
                    {
                        if (plannedItems[read] is { } item) sortedItems[write++] = item;
                    }
                    positionsChanged = false;
                    for (var slot = 0; slot < plannedItems.Length; slot++)
                    {
                        if (ReferenceEquals(plannedItems[slot], sortedItems[slot])) continue;
                        positionsChanged = true;
                        break;
                    }
                    plannedItems = sortedItems;
                }

                if (changedSlots.Count > 0)
                {
                    _items.ReplaceState(plannedItems);
                    mutation.RecordChanges(positionsChanged ? null : changedSlots);
                }
            }
        }

        private static IItem CloneWithCount(IItem item, int count)
        {
            var clone = item.Clone();
            if (ReferenceEquals(clone, item))
                throw new InvalidOperationException("Item cloning must produce a new instance for stock normalization.");
            clone.Count = count;
            return clone;
        }

        private void DecreaseStockItem(IItem?[] currentItems, IItem?[] plannedItems, int slot,
            HashSet<int> changedSlots, ref bool positionsChanged)
        {
            var item = currentItems[slot]!;
            var nextCount = item.Count - 1;
            if (nextCount <= 0 && _items.CountToResetTo == -1)
            {
                plannedItems[slot] = null;
                positionsChanged = true;
            }
            else
            {
                plannedItems[slot] = CloneWithCount(item, nextCount <= 0 ? _items.CountToResetTo : nextCount);
            }
            changedSlots.Add(slot);
        }
    }
}
