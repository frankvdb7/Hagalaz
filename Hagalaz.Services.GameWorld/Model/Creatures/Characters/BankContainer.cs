using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Resources;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Class BankContainer
    /// </summary>
    public class BankContainer : BaseItemContainer, IBankContainer, IHydratable<IReadOnlyList<HydratedItemDto>>, IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        /// The maximum capacity.
        /// </summary>
        private readonly int _maximumCapacity;

        /// <summary>
        /// Contstructs a container for character banks.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        /// <param name="capacity">The capacity of the container.</param>
        public BankContainer(ICharacter owner, int capacity)
            : base(StorageType.AlwaysStack, capacity)
        {
            _owner = owner;
            _itemBuilder = owner.ServiceProvider.GetRequiredService<IItemBuilder>();
            _maximumCapacity = capacity;
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public override void OnUpdate(HashSet<int>? slots = null) => _owner.EventManager.SendEvent(new BankChangedEvent(_owner, slots));

        /// <summary>
        /// Attempts to find a free slot in the container.
        /// </summary>
        /// <returns>Returns the availible slot id; -1 if none.</returns>
        public override int GetFreeSlot()
        {
            for (var i = 0; i < _maximumCapacity; i++)
            {
                if (Items[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

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
            if (!HasSpaceFor(deposited))
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
            if (Add(deposited))
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
        public bool DepositFromFamiliar(IItem item, int count, [NotNullWhen(true)] out IItem? deposited, IItemContainer container)
        {
            var slot = container.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
            {
                deposited = null;
                return false;
            }

            var toRemove = item.Clone(count);
            if (toRemove.ItemDefinition.Noted && toRemove.ItemDefinition.NoteId != -1)
            {
                deposited = _itemBuilder.Create().WithId(toRemove.ItemDefinition.NoteId).WithCount(count).Build();
            }
            else
            {
                deposited = _itemBuilder.Create().WithId(toRemove.Id).WithCount(count).Build();
            }
            if (!HasSpaceFor(deposited))
            {
                _owner.SendChatMessage("Not enough space in your bank.");
                deposited = null;
                return false;
            }

            var removed = container.Remove(toRemove, slot);
            deposited.Count = removed;
            if (Add(deposited))
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

            var toRemove = item.Clone(count);
            if (toRemove.ItemDefinition.Noted && toRemove.ItemDefinition.NoteId != -1)
            {
                deposited = _itemBuilder.Create().WithId(toRemove.ItemDefinition.NoteId).WithCount(count).Build();
            }
            else
            {
                deposited = _itemBuilder.Create().WithId(toRemove.Id).WithCount(count).Build();
            }
            if (!HasSpaceFor(deposited))
            {
                _owner.SendChatMessage("Not enough space in your bank.");
                deposited = null;
                return false;
            }

            var removed = _owner.Equipment.Remove(toRemove, slot);
            deposited.Count = removed;
            if (Add(deposited))
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
        /// <returns>If depositing was sucessfull.</returns>
        public bool DepositFromInventory(IItem item, int count, [NotNullWhen(true)] out IItem? deposited)
        {
            var slot = _owner.Inventory.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
            {
                deposited = null;
                return false;
            }

            var toRemove = item.Clone(count);
            if (toRemove.ItemDefinition.Noted && toRemove.ItemDefinition.NoteId != -1)
            {
                deposited = _itemBuilder.Create().WithId(toRemove.ItemDefinition.NoteId).WithCount(count).Build();
            }
            else
            {
                deposited = _itemBuilder.Create().WithId(toRemove.Id).WithCount(count).Build();
            }

            if (!HasSpaceFor(deposited))
            {
                _owner.SendChatMessage("Not enough space in your bank.");
                deposited = null;
                return false;
            }

            var removed = _owner.Inventory.Remove(toRemove, slot);
            deposited.Count = removed;
            if (Add(deposited))
            {
                return true;
            }

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
            var slot = GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
            {
                withdrawed = null;
                return false;
            }

            var toRemove = item.Clone();
            if (toRemove.Count < count)
            {
                count = toRemove.Count;
            }
            toRemove.Count = count;
            withdrawed = toRemove.Clone(count);
            if (notingEnabled)
            {
                if (!toRemove.ItemDefinition.Noted && toRemove.ItemDefinition.NoteId != -1)
                {
                    withdrawed = _itemBuilder.Create().WithId(toRemove.ItemDefinition.NoteId).WithCount(count).Build();
                }
                else
                {
                    _owner.SendChatMessage("This item cannot be withdrawn as note.");
                }
            }

            var stack = toRemove.ItemDefinition.Stackable || toRemove.ItemDefinition.Noted;
            var needSlots = 0;
            if (stack)
            {
                if (_owner.Inventory.GetSlotByItem(toRemove) != -1)
                {
                    long total = _owner.Inventory.GetCount(toRemove) + (long)count;
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
            if ((freeSlots = _owner.Inventory.FreeSlots) < needSlots)
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

            var removed = Remove(toRemove, slot);
            if (removed <= 0)
            {
                return false;
            }

            withdrawed.Count = removed;
            if (!_owner.Inventory.Add(withdrawed))
            {
                return false;
            }

            Sort();
            return true;

        }

        public void Hydrate(IReadOnlyList<HydratedItemDto> bank)
        {
            var items = new IItem[Capacity];
            foreach (var hydrated in bank)
            {
                var builder = _itemBuilder.Create().WithId(hydrated.ItemId).WithCount(hydrated.Count);
                if (!string.IsNullOrEmpty(hydrated.ExtraData))
                {
                    builder.WithExtraData(hydrated.ExtraData);
                }
                items[hydrated.SlotId] = builder.Build();
            }
            SetItems(items, false);
        }

        public IReadOnlyList<HydratedItemDto> Dehydrate()
        {
            var items = ToArray();
            return items
                .OfType<IItem>()
                .Select((item, index) => new HydratedItemDto(item.Id, item.Count, index, item.SerializeExtraData()))
                .ToList();
        }
    }
}