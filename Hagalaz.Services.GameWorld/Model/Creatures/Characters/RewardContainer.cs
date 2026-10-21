using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Logic.Characters;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public partial class RewardContainer : IRewardContainer, IItemTransactionSource, IItemContainerCompletionOwner,
        IHydratable<IReadOnlyList<HydratedItemDto>>,
        IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;

        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        /// Contstructs a container for character ingame mail.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        private readonly ItemContainer _items;
        IReadOnlyList<ItemContainerSynchronization> IItemTransactionSource.Synchronizations =>
            ((IItemTransactionSource)_items).Synchronizations;
        public IReadOnlyItemContainer Items => _items;

        public RewardContainer(ICharacter owner, IItemBuilder itemBuilder)
        {
            _owner = owner;
            _itemBuilder = itemBuilder;
            var storage = new ItemContainerStorage(StorageType.AlwaysStack, byte.MaxValue);
            var synchronization = new ItemContainerSynchronization(storage, this);
            _items = new ItemContainer(synchronization);
        }

        /// <summary>
        /// Withdraws from reward container.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public int Claim(IItem item, int count)
        {
            var slot = Items.GetInstanceSlot(item);
            if (slot == -1 || count <= 0) return -1;
            var toRemove = item.Clone();
            if (toRemove.Count < count) count = toRemove.Count;
            toRemove.Count = count;

            var canTransfer = _owner.Inventory.Items.TryGetTransferableItemCount(toRemove, count,
                out var transferCount, out var wasLimitedByFreeSlots);
            if (wasLimitedByFreeSlots)
            {
                _owner.SendChatMessage(GameStrings.InventoryFull);
            }

            if (!canTransfer) return -1;

            count = transferCount;
            if (wasLimitedByFreeSlots) toRemove.Count = count;

            using var transaction = ItemContainerTransaction.Begin(_items, _owner.Inventory);
            if (!_owner.Inventory.TryTransferItemFrom(_items, item, count, slot))
            {
                return -1;
            }

            _items.Sort();
            transaction.Commit();
            return count;
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        void IItemContainerCompletionOwner.PublishCommittedChanges()
        {
            var slots = _items.ChangeTracker.GetSlots();
            if (slots is { Count: 0 }) return;
            _owner.EventManager.SendEvent(new RewardsChangedEvent(_owner, slots));
        }

        public void Hydrate(IReadOnlyList<HydratedItemDto> rewards)
        {
            _items.RestoreItems(rewards.Select(entry => entry.ToStorageEntry(_itemBuilder)));
        }

        public IReadOnlyList<HydratedItemDto> Dehydrate()
        {
            return _items.ToHydratedItems();
        }
    }
}
