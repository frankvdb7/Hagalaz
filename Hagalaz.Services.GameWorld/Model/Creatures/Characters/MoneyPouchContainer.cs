using System.Collections.Concurrent;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Utilities;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MoneyPouchContainer : IMoneyPouchContainer, IItemContainerCompletionOwner,
        IHydratable<IReadOnlyList<HydratedItemDto>>,
        IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;

        private readonly IItemBuilder _itemBuilder;
        private readonly ItemContainerStorage _storage;
        private readonly ItemContainerMutationBoundary _storageMutations;
        private readonly MutationBoundary _mutations;
        private readonly ConcurrentDictionary<ItemContainerTransaction, Queue<MoneyPouchChange>> _pendingChanges = new();
        private readonly record struct MoneyPouchChange(int Order, int PreviousCount, int NewCount, int ChangeCount);

        public IMoneyPouchMutationBoundary Mutations => _mutations;

        public bool HasSpaceForCoins(int count) => count > 0 && (long)Count + count <= int.MaxValue;

        public bool HasCoins(int count) => count > 0 && (long)Count + _owner.Inventory.Items.GetCountById(995) >= count;

        /// <summary>
        /// Contains the money count.
        /// </summary>
        public int Count => _storage[0]!.Count;

        /// <summary>
        /// Gets the examine.
        /// </summary>
        /// <value>
        /// The examine.
        /// </value>
        public string Examine
        {
            get
            {
                if (Count == 1) return "Your money pouch currently contains 1 coin.";
                return "Your money pouch currently contains " + StringUtilities.FormatNumber(Count) + " coins.";
            }
        }

        /// <summary>
        /// Contstructs a container for character inventories.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        public MoneyPouchContainer(ICharacter owner, IItemBuilder itemBuilder)
        {
            _owner = owner;
            _itemBuilder = itemBuilder;
            var coins = _itemBuilder.Create().WithId(995).WithCount(0).Build();
            _storage = new ItemContainerStorage(StorageType.Normal, [coins], 1, 0);
            _storageMutations = new ItemContainerMutationBoundary(_storage, null, this);
            _mutations = new MutationBoundary(this);
        }

        /// <summary>
        /// Adds the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public bool Add(int count)
        {
            return TryAddExact(count);
        }

        /// <summary>
        /// Adds coins using the normal pouch overflow behavior as one exact
        /// operation. This method acquires the pouch and inventory mutation
        /// boundaries together.
        /// </summary>
        public bool TryAddExact(int count)
        {
            using var transaction = ItemContainerTransaction.Begin(Mutations);
            if (!Mutations.TryAddExact(count)) return false;
            transaction.Commit();
            return true;
        }

        private bool AddExactCore(int count)
        {
            if (count <= 0)
            {
                return false;
            }

            var pouchCount = Math.Min(count, int.MaxValue - Count);
            var inventoryCount = count - pouchCount;
            if (inventoryCount > 0 && !_owner.Inventory.Items.HasSpaceFor(
                    _itemBuilder.Create().WithId(995).WithCount(inventoryCount).Build()))
            {
                return false;
            }

            var previousCount = Count;
            if (pouchCount > 0 && !_storage.TryAddRange(
                    [_itemBuilder.Create().WithId(995).WithCount(pouchCount).Build()], out _))
            {
                return false;
            }

            if (inventoryCount > 0 && !_owner.Inventory.Items.Mutations.TryAddRange(
                    [_itemBuilder.Create().WithId(995).WithCount(inventoryCount).Build()]))
            {
                return false;
            }

            if (pouchCount != 0) DeferChange(previousCount, Count, pouchCount);
            return true;
        }

        /// <summary>
        /// Removes the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public int Remove(int count)
        {
            if (count <= 0) return 0;
            var amount = (int)Math.Min((long)count, (long)Count + _owner.Inventory.Items.GetCountById(995));
            return amount > 0 && TryRemoveExact(amount) ? amount : 0;
        }

        /// <summary>
        /// Removes coins using the normal pouch underflow behavior as one exact
        /// operation. This method acquires the pouch and inventory mutation
        /// boundaries together.
        /// </summary>
        public bool TryRemoveExact(int count)
        {
            using var transaction = ItemContainerTransaction.Begin(Mutations);
            if (!Mutations.TryRemoveExact(count)) return false;
            transaction.Commit();
            return true;
        }

        private bool RemoveExactCore(int count)
        {
            if (count <= 0)
            {
                return false;
            }

            var pouchCount = Math.Min(count, Count);
            var inventoryCount = count - pouchCount;
            if (inventoryCount > _owner.Inventory.Items.GetCountById(995))
            {
                return false;
            }

            var previousCount = Count;
            if (pouchCount > 0 && !_storage.TryRemoveExact(
                    _itemBuilder.Create().WithId(995).WithCount(pouchCount).Build(), 0, out _))
            {
                return false;
            }

            if (inventoryCount > 0 && !_owner.Inventory.Items.Mutations.TryRemoveExact(
                    _itemBuilder.Create().WithId(995).WithCount(inventoryCount).Build()))
            {
                return false;
            }

            DeferChange(previousCount, Count, -count);
            return true;
        }

        private void DeferChange(int previousCount, int newCount, int changeCount)
        {
            var transaction = _storage.Transaction ?? throw new InvalidOperationException("Pouch changes require an active transaction.");
            var changes = _pendingChanges.GetOrAdd(transaction, _ => new Queue<MoneyPouchChange>());
            changes.Enqueue(new MoneyPouchChange(transaction.NextCompletionOrder(), previousCount, newCount, changeCount));
        }

        void IItemContainerCompletionOwner.DiscardPendingCompletion(ItemContainerTransaction transaction) => _pendingChanges.TryRemove(transaction, out _);
        void IItemContainerCompletionOwner.CompleteBeforePublication(ItemContainerTransaction transaction, int order) { }
        void IItemContainerCompletionOwner.CompleteAfterPublication(ItemContainerTransaction transaction, int order)
        {
            if (!_pendingChanges.TryGetValue(transaction, out var changes) || !changes.TryPeek(out var change) || change.Order != order) return;
            changes.Dequeue(); // Consume before any observable code; a failure is never retried.
            PublishChange(change);
        }

        private void PublishChange(MoneyPouchChange change)
        {
            SendMoneyPouchChangedMessage(change.ChangeCount);
            if (change.PreviousCount != change.NewCount)
                _owner.EventManager.SendEvent(new MoneyPouchChangedEvent(_owner, change.PreviousCount, change.NewCount));
        }

        /// <summary>
        /// Sends the money pouch changed message.
        /// </summary>
        /// <param name="changeCount">The change count.</param>
        private void SendMoneyPouchChangedMessage(int changeCount)
        {
            switch (changeCount)
            {
                case < 0: _owner.SendChatMessage(StringUtilities.FormatNumber(-changeCount) + " coins have been removed from your money pouch."); break;
                case > 0: _owner.SendChatMessage(StringUtilities.FormatNumber(changeCount) + " coins have been added to your money pouch."); break;
            }
        }

        /// <summary>
        /// Adds from inventory.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public bool AddFromInventory(int count)
        {
            if (count <= 0) return false;

            var remainingSpace = int.MaxValue - Count;
            if (count > remainingSpace)
            {
                _owner.SendChatMessage(GameStrings.MoneyPouchFull);
            }

            var transferCount = Math.Min(count, Math.Min(remainingSpace, _owner.Inventory.Items.GetCountById(995)));
            if (transferCount <= 0) return false;

            using var transaction = ItemContainerTransaction.Begin(_owner.Inventory.Items.Mutations, Mutations);
            if (!_owner.Inventory.Items.Mutations.TryRemoveExact(
                    _itemBuilder.Create().WithId(995).WithCount(transferCount).Build()) ||
                !Mutations.TryAddExact(transferCount)) return false;
            transaction.Commit();
            return true;
        }

        /// <summary>
        /// Moves to inventory.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public bool MoveToInventory(int count)
        {
            var inventoryFull = false;
            using (var transaction = ItemContainerTransaction.Begin(_owner.Inventory.Items.Mutations, Mutations))
            {
                if (TryMoveToInventoryCore(count, out inventoryFull))
                {
                    transaction.Commit();
                    return true;
                }
            }
            if (inventoryFull) _owner.SendChatMessage(GameStrings.InventoryFull);
            return false;
        }

        private bool TryMoveToInventoryCore(int count, out bool inventoryFull)
        {
            inventoryFull = false;
            if (count <= 0) return false;
            var transferCount = Math.Min(count, Count);
            if (transferCount <= 0) return false;
            if (!_owner.Inventory.Items.Mutations.TryAddRange([_itemBuilder.Create().WithId(995).WithCount(transferCount).Build()]))
            {
                inventoryFull = true;
                return false;
            }
            return Mutations.TryRemoveExact(transferCount);
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public void Hydrate(IReadOnlyList<HydratedItemDto> moneyPouch)
        {
            if (moneyPouch.Any(item => item.ItemId != 995))
            {
                throw new ArgumentException("Money pouch state must contain coins.", nameof(moneyPouch));
            }

            if (moneyPouch.Any(item => item.SlotId != 0))
            {
                throw new ArgumentOutOfRangeException(nameof(moneyPouch), "Money pouch coins must occupy slot 0.");
            }

            var items = moneyPouch.Count == 0
                ? new[] { (0, _itemBuilder.Create().WithId(995).WithCount(0).Build()) }
                : moneyPouch.Select(entry => (entry.SlotId,
                    _itemBuilder.Create().WithId(995).WithCount(entry.Count)
                        .WithExtraData(entry.ExtraData ?? string.Empty).Build())).ToArray();
            _storage.RestoreItems(items, allowZeroCount: true);
        }

        public IReadOnlyList<HydratedItemDto> Dehydrate() => new[] { _storage[0]! }
            .Select((item, slot) => new HydratedItemDto(item.Id, item.Count, slot, item.SerializeExtraData()))
            .ToArray();

        private sealed class MutationBoundary(MoneyPouchContainer owner) : IMoneyPouchMutationBoundary,
            IItemContainerTransactionParticipantInternal
        {
            public IReadOnlyList<ItemContainerMutationBoundary> Boundaries
            {
                get
                {
                    if (owner._owner.Inventory.Items.Mutations is not IItemContainerTransactionParticipantInternal inventory)
                        throw new ArgumentException("Use the participant provided by a container's Mutations property.", "participants");
                    var boundaries = inventory.Boundaries;
                    if (boundaries is null || boundaries.Count == 0)
                        throw new ArgumentException("The inventory participant must contribute storage.", "participants");
                    return [owner._storageMutations, .. boundaries];
                }
            }

            public bool TryAddExact(int count)
            {
                EnsureCompleteTransaction();
                return owner.AddExactCore(count);
            }

            public bool TryRemoveExact(int count)
            {
                EnsureCompleteTransaction();
                return owner.RemoveExactCore(count);
            }

            private ItemContainerTransaction EnsureCompleteTransaction()
            {
                var transaction = owner._storage.Transaction
                    ?? throw new InvalidOperationException("Money pouch storage must belong to an active transaction.");
                transaction.EnsureActive();
                foreach (var boundary in Boundaries)
                {
                    if (!ReferenceEquals(boundary.Storage.Transaction, transaction))
                        throw new InvalidOperationException("The active transaction must include every money pouch storage boundary.");
                }
                return transaction;
            }
        }

    }
}
