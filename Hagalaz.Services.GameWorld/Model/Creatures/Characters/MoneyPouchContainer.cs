using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Utilities;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MoneyPouchContainer : IMoneyPouchContainer, IHydratable<IReadOnlyList<HydratedItemDto>>,
        IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;

        private readonly IItemBuilder _itemBuilder;
        private readonly ItemContainerStorage _storage;
        private readonly ItemContainerMutationBoundary _mutations;

        public bool HasSpaceForCoins(int count) => count > 0 && (long)Count + count <= int.MaxValue;

        /// <summary>
        /// The previous count
        /// </summary>
        private int _previousCount;

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
            _mutations = new ItemContainerMutationBoundary(_storage, null);
        }

        /// <summary>
        /// Adds the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public bool Add(int count)
        {
            var totalCount = (ulong)count + (ulong)Count;
            if (totalCount > int.MaxValue)
            {
                var remainingSpace = int.MaxValue - Count;
                if (remainingSpace > 0)
                {
                    _previousCount = Count;
                    SendMoneyPouchChangedMessage(remainingSpace);
                    TryAddToPouch(_itemBuilder.Create().WithId(995).WithCount(remainingSpace).Build());
                }

                var inventoryCount = count - remainingSpace;
                return count <= 0 || _owner.Inventory.Items.Add(_itemBuilder.Create().WithId(995).WithCount(inventoryCount).Build());
            }

            _previousCount = Count;
            SendMoneyPouchChangedMessage(count);
            return TryAddToPouch(_itemBuilder.Create().WithId(995).WithCount(count).Build());
        }

        /// <summary>
        /// Adds coins using the normal pouch overflow behavior as one exact
        /// operation. This method acquires the pouch and inventory mutation
        /// boundaries together.
        /// </summary>
        public bool TryAddExact(int count)
        {
            var transaction = new ItemContainerTransaction();
            EnlistIn(transaction);
            return transaction.TryExecute(tx => TryStageAddExact(tx, count));
        }

        public bool TryStageAddExact(IItemContainerTransaction transaction, int count)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            return TryStageExactAddCore(transaction, count);
        }

        private bool TryStageExactAddCore(IItemContainerTransaction transaction, int count)
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
            if (pouchCount > 0 && !transaction.TryAddRange(_mutations,
                    [_itemBuilder.Create().WithId(995).WithCount(pouchCount).Build()]))
            {
                return false;
            }

            if (inventoryCount > 0 && !transaction.TryAddRange(_owner.Inventory.Items.Mutations,
                    [_itemBuilder.Create().WithId(995).WithCount(inventoryCount).Build()]))
            {
                return false;
            }

            if (pouchCount > 0) transaction.OnCommitted(() =>
            {
                _previousCount = previousCount;
                PublishChanges(pouchCount);
            });
            return true;
        }

        /// <summary>
        /// Removes the specified count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public int Remove(int count)
        {
            if (count > Count)
            {
                var remaining = count - Count;
                var inventoryCount = _owner.Inventory.Items.GetCountById(995);
                if (remaining > inventoryCount) remaining = inventoryCount;
                _previousCount = Count;
                var removed = 0;
                if (Count > 0) removed += RemoveFromPouch(_itemBuilder.Create().WithId(995).WithCount(Count).Build(), 0);
                if (remaining > 0) removed += _owner.Inventory.Items.Remove(_itemBuilder.Create().WithId(995).WithCount(remaining).Build());
                SendMoneyPouchChangedMessage(-removed);
                return removed;
            }

            _previousCount = Count;
            SendMoneyPouchChangedMessage(-count);
            return RemoveFromPouch(_itemBuilder.Create().WithId(995).WithCount(count).Build(), 0);
        }

        /// <summary>
        /// Removes coins using the normal pouch underflow behavior as one exact
        /// operation. This method acquires the pouch and inventory mutation
        /// boundaries together.
        /// </summary>
        public bool TryRemoveExact(int count)
        {
            var transaction = new ItemContainerTransaction();
            EnlistIn(transaction);
            return transaction.TryExecute(tx => TryStageRemoveExact(tx, count));
        }

        /// <summary>
        /// Stages an exact coin removal without publishing pouch or inventory updates.
        /// </summary>
        public bool TryStageRemoveExact(IItemContainerTransaction transaction, int count)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            return TryStageExactRemoveCore(transaction, count);
        }

        private bool TryStageExactRemoveCore(IItemContainerTransaction transaction, int count)
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
            if (pouchCount > 0 && !transaction.TryRemoveExact(_mutations,
                    _itemBuilder.Create().WithId(995).WithCount(pouchCount).Build(), 0))
            {
                return false;
            }

            if (inventoryCount > 0 && !transaction.TryRemoveExact(_owner.Inventory.Items.Mutations,
                    _itemBuilder.Create().WithId(995).WithCount(inventoryCount).Build()))
            {
                return false;
            }

            transaction.OnCommitted(() =>
            {
                _previousCount = previousCount;
                PublishChanges(-count);
            });
            return true;
        }

        private void PublishChanges(int pouchChangeCount)
        {
            SendMoneyPouchChangedMessage(pouchChangeCount);
            OnUpdate();
        }

        public void EnlistIn(IItemContainerTransaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            transaction.Include(_mutations);
            transaction.Include(_owner.Inventory.Items.Mutations);
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
            var totalCount = (ulong)count + (ulong)Count;
            if (totalCount > int.MaxValue)
            {
                count = int.MaxValue - Count;
                _owner.SendChatMessage(GameStrings.MoneyPouchFull);
            }

            if (count <= 0) return false;
            var remove = _itemBuilder.Create().WithId(995).WithCount(count).Build();
            var removed = _owner.Inventory.Items.Remove(remove);
            return removed > 0 && Add(removed);
        }

        /// <summary>
        /// Moves to inventory.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public bool MoveToInventory(int count)
        {
            _previousCount = Count;
            var remove = _itemBuilder.Create().WithId(995).WithCount(count).Build();
            if (!_owner.Inventory.Items.HasSpaceFor(remove))
            {
                _owner.SendChatMessage(GameStrings.InventoryFull);
                return false;
            }

            var removed = RemoveFromPouch(remove, 0);
            if (removed <= 0) return false;
            var add = _itemBuilder.Create().WithId(995).WithCount(removed).Build();
            if (!_owner.Inventory.Items.Add(add))
            {
                _owner.SendChatMessage(GameStrings.InventoryFull);
                TryAddToPouch(add);
                return false;
            }

            SendMoneyPouchChangedMessage(-removed);
            return true;
        }

        /// <summary>
        /// Whether the container contains a certain Item.
        /// </summary>
        /// <param name="id">The Item id.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// Returns true if contained; false otherwise.
        /// </returns>
        public bool Contains(int id, int count)
        {
            if (id == 995)
            {
                var availableCoins = (long)Count + _owner.Inventory.Items.GetCountById(995);
                return availableCoins >= count;
            }

            return !_storage.Contains(id, count) && _owner.Inventory.Items.Contains(id, count);
        }

        public bool Contains(int id) => _storage.Contains(id);

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public void OnUpdate(HashSet<int>? slots = null)
        {
            if (_previousCount != Count)
            {
                var previousCount = _previousCount;
                _owner.EventManager.SendEvent(new MoneyPouchChangedEvent(_owner, previousCount, Count));
                _previousCount = Count;
            }
        }

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

        private bool TryAddToPouch(IItem item)
        {
            if (!_storage.TryAdd(item, out _)) return false;
            OnUpdate();
            return true;
        }

        private int RemoveFromPouch(IItem item, int preferredSlot)
        {
            var removed = _storage.Remove(item, preferredSlot, out _);
            if (removed > 0) OnUpdate();
            return removed;
        }
    }
}
