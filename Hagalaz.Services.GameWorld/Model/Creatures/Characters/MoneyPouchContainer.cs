using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Utilities;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MoneyPouchContainer : IMoneyPouchContainer, IItemTransactionSource, IItemContainerCompletionOwner,
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
        private readonly Queue<MoneyPouchChange> _pendingChanges = new();
        private readonly record struct MoneyPouchChange(int PreviousCount, int NewCount, int ChangeCount);

        IReadOnlyList<ItemContainerMutationBoundary> IItemTransactionSource.Boundaries
        {
            get
            {
                if (_owner.Inventory.Items is not IItemTransactionSource inventory)
                    throw new ArgumentException("The inventory must be an item-transactional domain object.", "participants");
                var boundaries = inventory.Boundaries;
                if (boundaries == null || boundaries.Count == 0)
                    throw new ArgumentException("The inventory participant must contribute storage.", "participants");
                if (boundaries.Any(boundary => boundary == null))
                    throw new ArgumentException("The inventory participant contributed a null boundary.", "participants");
                return [_storageMutations, .. boundaries];
            }
        }

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
            if (GetCompleteTransaction() is { } transaction) return AddExactCore(count, transaction);

            using var ownedTransaction = ItemContainerTransaction.Begin(this);
            if (!AddExactCore(count, ownedTransaction)) return false;
            ownedTransaction.Commit();
            return true;
        }

        private bool AddExactCore(int count, ItemContainerTransaction transaction)
        {
            if (!TryPlanExactAdd(count, out var pouchCount, out var inventoryCount)) return false;

            return ApplyExactAdd(transaction, pouchCount, inventoryCount);
        }

        private bool TryPlanExactAdd(int count, out int pouchCount, out int inventoryCount)
        {
            pouchCount = 0;
            inventoryCount = 0;
            if (count <= 0) return false;

            pouchCount = Math.Min(count, int.MaxValue - Count);
            inventoryCount = count - pouchCount;
            return inventoryCount <= 0 || _owner.Inventory.Items.HasSpaceFor(
                _itemBuilder.Create().WithId(995).WithCount(inventoryCount).Build());
        }

        private bool ApplyExactAdd(ItemContainerTransaction transaction, int pouchCount, int inventoryCount)
        {
            var previousCount = Count;
            using var pouchMutation = _storageMutations.BeginMutation();
            if (pouchCount > 0 && !_storage.TryAddRange(
                    [_itemBuilder.Create().WithId(995).WithCount(pouchCount).Build()], out _))
            {
                return false;
            }

            if (inventoryCount > 0 && !_owner.Inventory.Items.AddRange(
                    [_itemBuilder.Create().WithId(995).WithCount(inventoryCount).Build()]))
            {
                return false;
            }

            if (pouchCount != 0) DeferChange(transaction, previousCount, Count, pouchCount);
            return true;
        }

        /// <inheritdoc />
        public bool TryTransferCoinsFrom(IItemContainer source, IItem coins, int count, int preferredSourceSlot = -1)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(coins);
            if (coins.Id != 995 || count <= 0) return false;

            var sourceBoundary = ItemContainerTransaction.ResolveSingleBoundary(source);
            var transaction = GetCompleteTransaction(sourceBoundary)
                ?? throw new InvalidOperationException("Source, pouch, and inventory storage must belong to one active transaction.");
            if (!TryPlanExactAdd(count, out var pouchCount, out var inventoryCount)) return false;

            using var sourceMutation = sourceBoundary.BeginMutation();
            if (!sourceBoundary.Storage.TryRemoveExact(coins, count, preferredSourceSlot,
                    out var changedSlots)) return false;
            sourceMutation.RecordChanges(changedSlots);
            return ApplyExactAdd(transaction, pouchCount, inventoryCount);
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
            if (GetCompleteTransaction() is { } transaction) return RemoveExactCore(count, transaction);

            using var ownedTransaction = ItemContainerTransaction.Begin(this);
            if (!RemoveExactCore(count, ownedTransaction)) return false;
            ownedTransaction.Commit();
            return true;
        }

        private bool RemoveExactCore(int count, ItemContainerTransaction transaction)
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
            using var pouchMutation = _storageMutations.BeginMutation();
            if (pouchCount > 0 && !_storage.TryRemoveExact(
                    _itemBuilder.Create().WithId(995).WithCount(pouchCount).Build(), 0, out _))
            {
                return false;
            }

            if (inventoryCount > 0 && !_owner.Inventory.Items.TryRemoveExact(
                    _itemBuilder.Create().WithId(995).WithCount(inventoryCount).Build()))
            {
                return false;
            }

            DeferChange(transaction, previousCount, Count, -count);
            return true;
        }

        private void DeferChange(ItemContainerTransaction transaction, int previousCount, int newCount, int changeCount)
        {
            _storageMutations.EnsureOwnedBy(transaction);
            _pendingChanges.Enqueue(new MoneyPouchChange(previousCount, newCount, changeCount));
        }

        void IItemContainerCompletionOwner.DiscardPendingCompletion() => _pendingChanges.Clear();
        void IItemContainerCompletionOwner.CompleteBeforePublication() { }
        void IItemContainerCompletionOwner.CompleteAfterPublication()
        {
            while (_pendingChanges.TryDequeue(out var change))
                PublishChange(change); // Consume before observable code; a failure is never retried.
        }

        private ItemContainerTransaction? GetCompleteTransaction(ItemContainerMutationBoundary? additionalBoundary = null)
        {
            ItemContainerTransaction? transaction = null;
            var unbound = 0;
            var boundaries = ((IItemTransactionSource)this).Boundaries;
            foreach (var boundary in boundaries)
            {
                var current = boundary.Transaction;
                if (current == null)
                {
                    unbound++;
                    continue;
                }

                if (transaction == null)
                {
                    transaction = current;
                }
                else if (!ReferenceEquals(transaction, current))
                {
                    throw new InvalidOperationException("Money pouch storage belongs to different transactions.");
                }
            }

            if (additionalBoundary != null)
            {
                var current = additionalBoundary.Transaction;
                if (current == null) unbound++;
                else if (transaction == null) transaction = current;
                else if (!ReferenceEquals(transaction, current))
                    throw new InvalidOperationException("Money pouch source storage belongs to a different transaction.");
            }

            if (transaction == null) return null;
            if (unbound != 0)
                throw new InvalidOperationException("Every required money pouch storage contribution must belong to the same transaction.");

            transaction.EnsureActive();
            return transaction;
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

            using var transaction = ItemContainerTransaction.Begin(_owner.Inventory.Items, this);
            if (!_owner.Inventory.Items.TryRemoveExact(
                    _itemBuilder.Create().WithId(995).WithCount(transferCount).Build()) ||
                !TryAddExact(transferCount)) return false;
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
            using (var transaction = ItemContainerTransaction.Begin(_owner.Inventory.Items, this))
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
            if (!_owner.Inventory.Items.AddRange([_itemBuilder.Create().WithId(995).WithCount(transferCount).Build()]))
            {
                inventoryFull = true;
                return false;
            }
            return TryRemoveExact(transferCount);
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
            using (var mutation = _storageMutations.BeginMutation())
                _storage.RestoreItems(items, allowZeroCount: true);
        }

        public IReadOnlyList<HydratedItemDto> Dehydrate() => new[] { _storage[0]! }
            .Select((item, slot) => new HydratedItemDto(item.Id, item.Count, slot, item.SerializeExtraData()))
            .ToArray();

    }
}
