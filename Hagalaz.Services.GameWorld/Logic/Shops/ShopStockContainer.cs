using System.Collections.Generic;
using System.Linq;
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
    public partial class ShopStockContainer : IShopStockContainer
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
        public IItemContainer Items => _items;

        /// <summary>
        /// The original stock of the shop.
        /// </summary>
        /// <value>The original stock.</value>
        private readonly IItem[] _originalStock;

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
            _items = new ItemContainer(type, stock, capacity, OnUpdate, 0);
            _shop = shop;
            _sampleContainer = sampleContainer;
            _itemRepository = itemRepository;
            _itemBuilder = itemBuilder;
            _eventManager = eventManager;
            _originalStock = stock.ToArray();
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public void OnUpdate(HashSet<int>? slots = null)
        {
            if (_sampleContainer)
                _eventManager.SendEvent(new ShopSampleStockChangedEvent(_shop, slots));
            else
                _eventManager.SendEvent(new ShopStockChangedEvent(_shop, slots));
        }

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
                ? [viewer.Inventory.Items, _items, viewer.MoneyPouch]
                : [viewer.Inventory.Items, _items];
            using var transaction = ItemContainerTransaction.Begin(participants);
            if (!viewer.Inventory.Items.TryTransferTo(_items, item, count, slot,
                    destinationItem: transformed ? sold : null)) return false;
            var paid = _shop.CurrencyId == 995
                ? viewer.MoneyPouch.TryAddExact((int)currencyCount)
                : viewer.Inventory.Items.Add(_itemBuilder.Create().WithId(_shop.CurrencyId).WithCount((int)currencyCount).Build());
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

            var originalStock = _originalStock.Any(it => it.Id == item.Id);
            IItemTransactional[] participants = _shop.CurrencyId == 995
                ? [_items, viewer.Inventory.Items, viewer.MoneyPouch]
                : [_items, viewer.Inventory.Items];
            var insufficientCurrency = false;
            using (var transaction = ItemContainerTransaction.Begin(participants))
            {
                if (cost > 0)
                {
                    var paid = _shop.CurrencyId == 995
                        ? viewer.MoneyPouch.TryRemoveExact((int)cost)
                        : viewer.Inventory.Items.TryRemoveExact(
                            _itemBuilder.Create().WithId(_shop.CurrencyId).WithCount((int)cost).Build());
                    insufficientCurrency = !paid;
                }
                if (!insufficientCurrency)
                {
                    if (!_items.TryTransferTo(viewer.Inventory.Items, item, count, slot)) return false;
                    transaction.Commit();
                }
            }
            if (insufficientCurrency)
            {
                viewer.SendChatMessage("You don't have enough " + _itemRepository.FindItemDefinitionById(_shop.CurrencyId).Name.ToLower() + "!");
                return false;
            }
            if (!originalStock && Items[slot] == null) Items.Sort();
            _eventManager.SendEvent(new ShopItemBoughtEvent(viewer, _shop, toRemove));
            return true;
        }

        /// <summary>
        /// Normalizes the stock.
        /// </summary>
        public void NormalizeStock()
        {
            var changedSlots = new HashSet<int>();
            var shouldSort = false;
            using (var mutation = _items.BeginMutation())
            {
                var items = _items.SnapshotItems();
                // This uses the full capacity, because we don't know if items were added.
                for (var i = 0; i < Items.Capacity; i++)
                {
                    var item = items[i];
                    if (item == null)
                    {
                        continue;
                    }

                    if (item.Count < _originalStock[i].Count)
                    {
                        item.Count += 1;
                        changedSlots.Add(i);
                    }
                    else if (item.Count > 0 && _originalStock.All(original => original.Id != item.Id))
                    {
                        item.Count -= 1;
                        changedSlots.Add(i);
                        if (item.Count <= 0)
                        {
                            if (_items.CountToResetTo == -1)
                            {
                                items[i] = null;
                            }
                            else
                            {
                                item.Count = _items.CountToResetTo;
                            }

                            shouldSort = true;
                        }
                    }
                }

                if (shouldSort)
                {
                    var write = 0;
                    for (var i = 0; i < items.Length; i++)
                    {
                        if (items[i] == null)
                        {
                            continue;
                        }

                        var item = items[i];
                        items[i] = null;
                        items[write++] = item;
                    }
                }

                if (changedSlots.Count > 0)
                {
                    _items.ReplaceState(items);
                    mutation.RecordChanges(shouldSort ? null : changedSlots);
                }
            }
        }
    }
}
