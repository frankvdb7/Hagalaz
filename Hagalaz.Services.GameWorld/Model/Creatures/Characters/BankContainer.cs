using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Logic.Characters;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Class BankContainer
    /// </summary>
    public partial class BankContainer : IBankContainer, IHydratable<IReadOnlyList<HydratedItemDto>>, IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;
        private readonly IItemBuilder _itemBuilder;

        private readonly ItemContainer _items;
        public ItemContainer Items => _items;

        /// <summary>
        /// Contstructs a container for character banks.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        /// <param name="capacity">The capacity of the container.</param>
        public BankContainer(ICharacter owner, int capacity, IItemBuilder itemBuilder)
        {
            _owner = owner;
            _itemBuilder = itemBuilder;
            _items = new ItemContainer(StorageType.AlwaysStack, capacity, OnUpdate);
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public void OnUpdate(HashSet<int>? slots = null) => _owner.EventManager.SendEvent(new BankChangedEvent(_owner, slots));

        /// <summary>
        /// Deposits from money pouch.
        /// </summary>
        /// <param name="deposited">The deposited.</param>
        /// <returns></returns>
        public bool DepositFromMoneyPouch([NotNullWhen(true)] out IItem? deposited)
        {
            var count = _owner.MoneyPouch.Count;
            if (count <= 0)
            {
                deposited = null;
                return false;
            }

            deposited = _itemBuilder.Create().WithId(995).WithCount(count).Build();
            if (!Items.HasSpaceFor(deposited))
            {
                _owner.SendChatMessage("Not enough space in your bank.");
                deposited = null;
                return false;
            }

            var removed = _owner.MoneyPouch.Remove(count);
            if (removed <= 0)
            {
                deposited = null;
                return false;
            }

            deposited.Count = removed;
            if (Items.Add(deposited))
            {
                return true;
            }

            deposited = null;
            return false;

        }

        /// <summary>
        /// Deposit's specific item into character's bank.
        /// </summary>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <param name="deposited">Pointer to item which was deposited into bank. Can be null.</param>
        /// <param name="container"></param>
        /// <returns>If depositing was sucessfull.</returns>
        public bool DepositFromFamiliar(IItem item, int count, [NotNullWhen(true)] out IItem? deposited, ItemContainer container)
        {
            var slot = container.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
            {
                deposited = null;
                return false;
            }

            count = Math.Min(count, container.GetCount(item));
            if (count <= 0)
            {
                deposited = null;
                return false;
            }

            deposited = CreateDepositItem(item, count, out var transformed);
            if (container.Mutations.TryTransferTo(_items.Mutations, item, count, slot,
                    destinationItem: transformed ? deposited : null))
            {
                return true;
            }

            _owner.SendChatMessage("Not enough space in your bank.");
            deposited = null;
            return false;
        }

        /// <summary>
        /// Deposit's specific item into character's bank.
        /// </summary>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <param name="deposited">Pointer to item which was deposited into bank. Can be null.</param>
        /// <returns>If depositing was sucessfull.</returns>
        public bool DepositFromEquipment(IItem item, int count, [NotNullWhen(true)] out IItem? deposited)
        {
            if (count <= 0)
            {
                deposited = null;
                return false;
            }

            var slot = _owner.Equipment.GetInstanceSlot(item);
            if (slot == EquipmentSlot.NoSlot)
            {
                deposited = null;
                return false;
            }

            var equipmentContainer = _owner.Equipment;
            if (equipmentContainer[(int)slot] is not { } equippedItem)
            {
                deposited = null;
                return false;
            }

            count = Math.Min(count, equippedItem.Count);
            if (count <= 0)
            {
                deposited = null;
                return false;
            }

            deposited = CreateDepositItem(equippedItem, count, out var transformed);
            if (_owner.Equipment.TryMoveTo(_items, equippedItem, count, slot,
                    transformed ? deposited : null))
            {
                return true;
            }

            _owner.SendChatMessage("Not enough space in your bank.");
            deposited = null;
            return false;

        }

        /// <summary>
        /// Deposit's specific item into character's bank.
        /// </summary>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <param name="deposited">Pointer to item which was deposited into bank. Can be null.</param>
        /// <returns>If depositing was sucessfull.</returns>
        public bool DepositFromInventory(IItem item, int count, [NotNullWhen(true)] out IItem? deposited)
        {
            var slot = _owner.Inventory.Items.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
            {
                deposited = null;
                return false;
            }

            count = Math.Min(count, _owner.Inventory.Items.GetCount(item));
            if (count <= 0)
            {
                deposited = null;
                return false;
            }

            deposited = CreateDepositItem(item, count, out var transformed);
            if (_owner.Inventory.Items.Mutations.TryTransferTo(_items.Mutations, item, count, slot,
                    destinationItem: transformed ? deposited : null))
            {
                return true;
            }

            _owner.SendChatMessage("Not enough space in your bank.");
            deposited = null;
            return false;

        }

        /// <summary>
        /// Withdraw's specific item from character's bank and
        /// stores it into character's inventory.
        /// </summary>
        /// <param name="item">Item in character's bank which will be withdrawed.</param>
        /// <param name="count">Count of how much items should be withdrawed.</param>
        /// <param name="notingEnabled">Wheter noting is enabled in bank.</param>
        /// <param name="withdrawed">Pointer to item which will be deposited into character's inventory.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool WithdrawFromBank(IItem item, int count, bool notingEnabled, [NotNullWhen(true)] out IItem? withdrawed)
        {
            var slot = Items.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
            {
                withdrawed = null;
                return false;
            }

            count = Math.Min(count, item.Count);
            if (count <= 0)
            {
                withdrawed = null;
                return false;
            }

            var toRemove = item.Clone(count);
            withdrawed = toRemove.Clone(count);
            var transformed = false;
            if (notingEnabled)
            {
                if (!toRemove.ItemDefinition.Noted && toRemove.ItemDefinition.NoteId != -1)
                {
                    withdrawed = _itemBuilder.Create().WithId(toRemove.ItemDefinition.NoteId).WithCount(count)
                        .WithExtraData(toRemove.SerializeExtraData() ?? string.Empty).Build();
                    transformed = true;
                }
                else
                {
                    _owner.SendChatMessage("This item cannot be withdrawn as note.");
                }
            }

            var stack = withdrawed.ItemDefinition.Stackable || withdrawed.ItemDefinition.Noted;
            var needSlots = 0;
            if (stack)
            {
                if (_owner.Inventory.Items.GetSlotByItem(withdrawed) != -1)
                {
                    long total = _owner.Inventory.Items.GetCount(withdrawed) + (long)count;
                    if (total > int.MaxValue)
                    {
                        withdrawed = null;
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
            if ((freeSlots = _owner.Inventory.Items.FreeSlots) < needSlots)
            {
                _owner.SendChatMessage(GameStrings.InventoryFull);
                if (stack || freeSlots <= 0) // we can't do anything since decreasing item count won't decrease needSlots.
                {
                    withdrawed = null;
                    return false;
                }

                count = freeSlots;
                withdrawed.Count = count;
                toRemove.Count = count;
            }

            withdrawed.Count = count;
            if (!_items.Mutations.TryTransferTo(_owner.Inventory.Items.Mutations, item, count, slot,
                    destinationItem: transformed ? withdrawed : null))
            {
                _owner.SendChatMessage(GameStrings.InventoryFull);
                withdrawed = null;
                return false;
            }

            Items.Sort();
            return true;

        }

        public void Hydrate(IReadOnlyList<HydratedItemDto> bank)
        {
            Items.Storage.RestoreItems(bank.Select(entry => entry.ToStorageEntry(_itemBuilder)));
        }

        public IReadOnlyList<HydratedItemDto> Dehydrate()
        {
            return Items.Storage.ToHydratedItems();
        }

        private IItem CreateDepositItem(IItem item, int count, out bool transformed)
        {
            var sourceItem = item.Clone(count);
            if (sourceItem.ItemDefinition.Noted && sourceItem.ItemDefinition.NoteId != -1)
            {
                transformed = true;
                return _itemBuilder.Create().WithId(sourceItem.ItemDefinition.NoteId).WithCount(count)
                    .WithExtraData(sourceItem.SerializeExtraData() ?? string.Empty).Build();
            }

            transformed = false;
            return sourceItem;
        }
    }
}
