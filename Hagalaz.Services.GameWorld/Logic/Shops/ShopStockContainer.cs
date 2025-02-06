using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;

namespace Hagalaz.Services.GameWorld.Logic.Shops
{
    /// <summary>
    /// Class ShopStockContainer
    /// </summary>
    public class ShopStockContainer : BaseItemContainer, IShopStockContainer
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
        public ShopStockContainer(
            IShop shop, IItemService itemRepository, IItemBuilder itemBuilder, bool sampleContainer, StorageType type, int capacity,
            IList<IItem> stock) : base(type, stock, capacity)
        {
            _shop = shop;
            _sampleContainer = sampleContainer;
            CountToResetTo = 0;
            _itemRepository = itemRepository;
            _itemBuilder = itemBuilder;
            _originalStock = stock.ToArray();
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public override void OnUpdate(HashSet<int>? slots = null)
        {
            if (_sampleContainer)
                new ShopSampleStockChangedEvent(_shop, slots).Send();
            else
                new ShopStockChangedEvent(_shop, slots).Send();
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
            var slot = viewer.Inventory.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
            {
                return false;
            }

            IItem sold;
            if (item.ItemDefinition.Noted && item.ItemDefinition.NoteId != -1)
            {
                sold = _itemBuilder.Create().WithId(item.ItemDefinition.NoteId).WithCount(count).Build();
            }
            else
            {
                sold = item.Clone(count);
            }

            if (!sold.ItemScript.CanSellItem(sold, viewer) || !_shop.GeneralStore && !Contains(sold))
            {
                viewer.SendChatMessage("You cannot sell this item.");
                return false;
            }

            if (!HasSpaceFor(sold))
            {
                viewer.SendChatMessage("There is not enough space in the shop for this item.");
                return false;
            }

            var toRemove = item.Clone(count);
            var removed = viewer.Inventory.Remove(toRemove, slot);
            sold.Count = removed;
            var currencyCount = sold.Count * (long)_shop.GetSellValue(item);
            if (currencyCount > int.MaxValue)
            {
                viewer.SendChatMessage("The shop does not have enough money for this amount of items.");
                return false;
            }

            if (!Add(sold))
            {
                return false;
            }

            return viewer.MoneyPouch.Contains(_shop.CurrencyId)
                ? viewer.MoneyPouch.Add((int)currencyCount)
                : viewer.Inventory.Add(_itemBuilder.Create().WithId(_shop.CurrencyId).WithCount((int)currencyCount).Build());
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
            var slot = GetInstanceSlot(item);
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
                if (viewer.Inventory.GetSlotByItem(toRemove) != -1)
                {
                    var total = viewer.Inventory.GetCount(toRemove) + (long)count;
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
            if ((freeSlots = viewer.Inventory.FreeSlots) < needSlots)
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

                var removed = 0;
                if (viewer.MoneyPouch.Contains(_shop.CurrencyId))
                {
                    removed = viewer.MoneyPouch.Remove((int)cost);
                }
                else if (viewer.Inventory.Contains(_shop.CurrencyId, (int)cost))
                {
                    removed = viewer.Inventory.Remove(_itemBuilder.Create().WithId(_shop.CurrencyId).WithCount((int)cost).Build());
                }

                if (removed <= 0)
                {
                    viewer.SendChatMessage("You don't have enough " + _itemRepository.FindItemDefinitionById(_shop.CurrencyId).Name.ToLower() + "!");
                    return false;
                }
            }

            if (_originalStock.Any(it => it.Id == item.Id))
            {
                Remove(toRemove, slot);
            }
            else
            {
                Remove(toRemove, slot);
                if (Items[slot] == null) Sort();
            }

            viewer.Inventory.Add(toRemove);
            new ShopItemBoughtEvent(viewer, _shop, toRemove).Send();
            return true;
        }

        /// <summary>
        /// Normalizes the stock.
        /// </summary>
        public void NormalizeStock()
        {
            var changedSlots = new HashSet<int>();
            // This uses the full capacity, because we don't know if items were added.
            for (var i = 0; i < Capacity; i++)
            {
                var item = Items[i];
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
                    if (item.Count <= 0)
                    {
                        Remove(item, i);
                        Sort();
                    }
                    else
                    {
                        changedSlots.Add(i);
                    }
                }
            }

            if (changedSlots.Count > 0)
            {
                OnUpdate(changedSlots);
            }
        }
    }
}