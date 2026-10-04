using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Resources;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public partial class FamiliarInventoryContainer : IFamiliarInventoryContainer, IHydratable<IReadOnlyList<HydratedItem>>, IDehydratable<IReadOnlyList<HydratedItem>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;
        private readonly IItemBuilder _itemBuilder;
        private readonly ItemContainer _items;
        public IItemContainer Items => _items;

        /// <summary>
        /// Constructs a container for character inventories.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        /// <param name="type">The type of container.</param>
        /// <param name="capacity">The capacity of the container.</param>
        public FamiliarInventoryContainer(ICharacter owner, StorageType type, int capacity, IItemBuilder itemBuilder)
        {
            (_owner, _itemBuilder) = (owner, itemBuilder);
            _items = new ItemContainer(type, capacity, OnUpdate);
        }

        /// <summary>
        /// Deposit's specific item into familiars inventory.
        /// </summary>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// If depositing was successful.
        /// </returns>
        public bool DepositFromInventory(IItem item, int count)
        {
            var slot = _owner.Inventory.Items.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
                return false;

            count = Math.Min(count, _owner.Inventory.Items.GetCount(item));
            if (count <= 0)
            {
                return false;
            }

            using (var transaction = ItemContainerTransaction.Begin(_owner.Inventory.Items, _items))
            {
                if (_owner.Inventory.Items.TryTransferTo(_items, item, count, slot))
                {
                    transaction.Commit();
                    return true;
                }
            }

            _owner.SendChatMessage(GameStrings.FamiliarInventoryFull);
            return false;
        }

        /// <summary>
        /// Withdraws from familiar inventory.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public bool WithdrawFromFamiliarInventory(IItem item, int count)
        {
            var slot = Items.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
                return false;

            count = Math.Min(count, Items.GetCount(item));
            if (count <= 0)
            {
                return false;
            }

            using (var transaction = ItemContainerTransaction.Begin(_items, _owner.Inventory.Items))
            {
                if (_items.TryTransferTo(_owner.Inventory.Items, item, count, slot))
                {
                    transaction.Commit();
                    return true;
                }
            }

            _owner.SendChatMessage(GameStrings.InventoryFull);
            return false;
        }

        public void WithdrawAvailableToInventory()
        {
            var inventoryItems = _owner.Inventory.Items;
            using var transaction = ItemContainerTransaction.Begin(_items, inventoryItems);
            var familiarItems = _items.Select((item, slot) => (item, slot))
                .Where(entry => entry.item is { Count: > 0 }).ToArray();
            foreach (var (item, slot) in familiarItems)
                _items.TryTransferTo(inventoryItems, item!, item!.Count, slot);
            transaction.Commit();
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public void OnUpdate(HashSet<int>? slots = null) => _owner.EventManager.SendEvent(new FamiliarInventoryChangedEvent(_owner, slots));

        public void Hydrate(IReadOnlyList<HydratedItem> inventory)
        {
            _items.RestoreItems(inventory.Select(entry => (entry.SlotId,
                _itemBuilder.Create().WithId(entry.ItemId).WithCount(entry.Count)
                    .WithExtraData(entry.ExtraData ?? string.Empty).Build())));
        }

        public IReadOnlyList<HydratedItem> Dehydrate() => _items.Select((item, slot) => (item, slot)).Where(x => x.item != null)
            .Select(entry => new HydratedItem(entry.item!.Id, entry.item.Count, entry.slot, entry.item.SerializeExtraData()))
            .ToArray();
    }
}
