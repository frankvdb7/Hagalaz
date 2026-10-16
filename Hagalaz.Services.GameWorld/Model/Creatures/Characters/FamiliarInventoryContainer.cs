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
    public partial class FamiliarInventoryContainer : IFamiliarInventoryContainer, IItemTransactionSource,
        IItemContainerCompletionOwner,
        IHydratable<IReadOnlyList<HydratedItem>>, IDehydratable<IReadOnlyList<HydratedItem>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;
        private readonly IItemBuilder _itemBuilder;
        private readonly ItemContainer _items;
        IReadOnlyList<ItemContainerSynchronization> IItemTransactionSource.Synchronizations =>
            ((IItemTransactionSource)_items).Synchronizations;
        public IReadOnlyItemContainer Items => _items;

        /// <summary>
        /// Constructs a container for character inventories.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        /// <param name="type">The type of container.</param>
        /// <param name="capacity">The capacity of the container.</param>
        public FamiliarInventoryContainer(ICharacter owner, StorageType type, int capacity, IItemBuilder itemBuilder)
        {
            (_owner, _itemBuilder) = (owner, itemBuilder);
            var storage = new ItemContainerStorage(type, capacity);
            var synchronization = new ItemContainerSynchronization(storage, this);
            _items = new ItemContainer(synchronization);
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

            if (_owner.Inventory.TryTransferItemTo(_items, item, count, slot)) return true;

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
            if (!CanWithdrawToInventory()) return false;

            var slot = Items.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
                return false;

            count = Math.Min(count, Items.GetCount(item));
            if (count <= 0)
            {
                return false;
            }

            if (_owner.Inventory.TryTransferItemFrom(_items, item, count, slot)) return true;

            _owner.SendChatMessage(GameStrings.InventoryFull);
            return false;
        }

        public void WithdrawAvailableToInventory()
        {
            if (!CanWithdrawToInventory()) return;

            using var transaction = ItemContainerTransaction.Begin(this, _owner.Inventory);
            var familiarItems = _items.Select((item, slot) => (item, slot))
                .Where(entry => entry.item is { Count: > 0 }).ToArray();
            foreach (var (item, slot) in familiarItems)
                _owner.Inventory.TryTransferItemFrom(_items, item!, item!.Count, slot);
            transaction.Commit();
        }

        private bool CanWithdrawToInventory()
        {
            var combat = _owner.Combat;
            if (!combat.IsDead && !combat.IsInCombat() && !_owner.GetScripts().Any(script => script.IsBusy()))
                return true;

            _owner.SendChatMessage("Please finish what you're doing before withdrawing items from your familiar.");
            return false;
        }

        public int RemoveItem(IItem item, int preferredSlot = -1, bool publishChanges = true)
        {
            return _items.Remove(item, preferredSlot, publishChanges);
        }

        public bool TryTransferItemTo(IItemContainer destination, IItem item, int count,
            int preferredSourceSlot = -1, int destinationSlot = -1, IItem? destinationItem = null)
        {
            return _items.TryTransferTo(destination, item, count, preferredSourceSlot, destinationSlot, destinationItem);
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        void IItemContainerCompletionOwner.PublishCommittedChanges()
        {
            var slots = _items.ChangeTracker.GetSlots();
            if (slots is { Count: 0 }) return;
            _owner.EventManager.SendEvent(new FamiliarInventoryChangedEvent(_owner, slots));
        }

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
