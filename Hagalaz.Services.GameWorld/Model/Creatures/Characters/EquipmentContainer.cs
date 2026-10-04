using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Logic.Characters;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Class EquipmentContainer
    /// </summary>
    public partial class EquipmentContainer : IEquipmentContainer, IItemTransactionSource, IItemContainerCompletionOwner, IHydratable<IReadOnlyList<HydratedItemDto>>,
        IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;
        private readonly IItemBuilder _itemBuilder;
        private readonly ItemContainerStorage _storage;
        private readonly ItemContainerMutationBoundary _mutations;
        IReadOnlyList<ItemContainerMutationBoundary> IItemTransactionSource.Boundaries => [_mutations];
        public IReadOnlyItemContainer Items { get; }
        private readonly ConcurrentDictionary<ItemContainerTransaction, Queue<EquipmentCompletion>> _pendingCompletion = new();
        private enum EquipmentEffectKind { Equipped, Unequipped, WeaponProfile }
        private readonly record struct EquipmentEffect(EquipmentEffectKind Kind, IItem Item, IItem? Incoming = null);
        private readonly record struct EquipmentCompletion(int Order, EquipmentEffect[] Effects);
        /// <summary>
        /// Gets the item by the specified array index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>Returns the Item object.</returns>
        public IItem? this[EquipmentSlot index] => _storage[(int)index];

        /// <summary>
        /// Constructs a container for character equipment.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        /// <param name="capacity">The capacity of the container.</param>
        public EquipmentContainer(ICharacter owner, int capacity, IItemBuilder itemBuilder)
        {
            (_owner, _itemBuilder) = (owner, itemBuilder);
            _storage = new ItemContainerStorage(StorageType.Normal, capacity);
            Items = new ReadOnlyItemContainer(_storage);
            _mutations = new ItemContainerMutationBoundary(_storage,
                slots => PublishCommittedChanges(slots?.Select(slot => (EquipmentSlot)slot).ToHashSet()), this);
        }

        /// <summary>
        /// Equips item to this character.
        /// </summary>
        /// <remarks>Script eligibility checks remain synchronous domain validation and can have their own side effects.</remarks>
        /// <param name="item">Item in inventory.</param>
        /// <returns>True if item was equipped sucessfully.</returns>
        public bool EquipItem(IItem item)
        {
            var slot = _owner.Inventory.Items.GetInstanceSlot(item);
            if (slot == -1) return false;
            if (!item.EquipmentScript.CanEquipItem(item, _owner)) return false;
            if (item.EquipmentDefinition.Slot == EquipmentSlot.NoSlot)
            {
                _owner.SendChatMessage("This item can't be equipped.");
                return false;
            }

            var equipSlot = item.EquipmentDefinition.Slot;
            var equipItem = this[equipSlot];
            if (equipItem != null && item.ItemScript.CanStackItem(item, equipItem, false))
            {
                long total = item.Count + (long)equipItem.Count;
                if (total > int.MaxValue)
                {
                    _owner.SendChatMessage("Too much items!");
                    return false;
                }

                using var transaction = ItemContainerTransaction.Begin(_owner.Inventory.Items, this);
                if (!MoveFromInventoryToSlot(item, slot, equipSlot)) return false;
                transaction.Commit();
                return true;
            }

            if (equipSlot != EquipmentSlot.Weapon && equipSlot != EquipmentSlot.Shield)
            {
                if (equipItem == null)
                {
                    using var transaction = ItemContainerTransaction.Begin(_owner.Inventory.Items, this);
                    if (!MoveFromInventoryToSlot(item, slot, equipSlot)) return false;
                    DeferEquipmentEffects(new EquipmentEffect(EquipmentEffectKind.Equipped, item));
                    transaction.Commit();
                    return true;
                }

                if (!equipItem.EquipmentScript.CanUnEquipItem(equipItem, _owner))
                {
                    return false;
                }

                // Custom unequip commands may open interactive UI and must remain outside mutation scopes.
                if (_owner.Inventory.Items.Remove(item, slot) <= 0)
                {
                    return false;
                }

                if (!equipItem.EquipmentScript.UnEquipItem(equipItem, _owner, slot))
                {
                    _owner.Inventory.Items.Add(slot, item);
                    return false;
                }

                if (_storage.TryAdd((int)equipSlot, item, out var changedSlots))
                {
                    PublishChanges(changedSlots.Select(changedSlot => (EquipmentSlot)changedSlot).ToHashSet());
                }

                item.EquipmentScript.OnEquipped(item, _owner);
                return true;
            }

            var equippedWeapon = this[EquipmentSlot.Weapon];
            var equippedShield = this[EquipmentSlot.Shield];
            if (equippedWeapon == null && equippedShield == null)
            {
                using var transaction = ItemContainerTransaction.Begin(_owner.Inventory.Items, this);
                if (!MoveFromInventoryToSlot(item, slot, equipSlot)) return false;
                DeferEquipmentEffects(new EquipmentEffect(EquipmentEffectKind.Equipped, item));
                transaction.Commit();
                return true;
            }

            var needsWeaponUnequip = equippedWeapon != null && (equipSlot == EquipmentSlot.Weapon ||
                                                                equipSlot == EquipmentSlot.Shield &&
                                                                equippedWeapon.EquipmentDefinition.Type == EquipmentType.TwoHanded);
            var needsShieldUnequip = equipSlot == EquipmentSlot.Shield
                ? equippedShield != null
                : equippedShield != null && item.EquipmentDefinition.Type == EquipmentType.TwoHanded;

            if (needsWeaponUnequip && !equippedWeapon!.EquipmentScript.CanUnEquipItem(equippedWeapon, _owner))
            {
                return false;
            }

            if (needsShieldUnequip && !equippedShield!.EquipmentScript.CanUnEquipItem(equippedShield, _owner))
            {
                return false;
            }

            var inventoryBoundary = ItemContainerTransaction.ResolveSingleBoundary(_owner.Inventory.Items);
            var inventoryFull = false;
            using (var replacementTransaction = ItemContainerTransaction.Begin(_owner.Inventory.Items, this))
            {
                if (!_owner.Inventory.Items.TryRemoveExact(item, slot)) return false;
                if (needsWeaponUnequip && !_mutations.TryTransferTo(inventoryBoundary, equippedWeapon!,
                        equippedWeapon!.Count, (int)EquipmentSlot.Weapon, slot))
                {
                    inventoryFull = true;
                }
                if (!inventoryFull && needsShieldUnequip && !_mutations.TryTransferTo(inventoryBoundary, equippedShield!,
                        equippedShield!.Count, (int)EquipmentSlot.Shield))
                {
                    inventoryFull = true;
                }
                if (!inventoryFull)
                {
                    if (!_storage.TryAdd((int)equipSlot, item, out var incomingSlots)) return false;
                    _mutations.NotifyChanges(incomingSlots);
                    var effects = new List<EquipmentEffect>();
                    if (needsWeaponUnequip) effects.Add(new EquipmentEffect(EquipmentEffectKind.Unequipped, equippedWeapon!));
                    if (needsShieldUnequip) effects.Add(new EquipmentEffect(EquipmentEffectKind.Unequipped, equippedShield!));
                    if (needsWeaponUnequip) effects.Add(new EquipmentEffect(EquipmentEffectKind.WeaponProfile, equippedWeapon!, item));
                    effects.Add(new EquipmentEffect(EquipmentEffectKind.Equipped, item));
                    DeferEquipmentEffects(effects.ToArray());
                    replacementTransaction.Commit();
                    return true;
                }
            }
            _owner.SendChatMessage("Not enough space in your inventory.");
            return false;
        }

        private void UpdateWeaponProfileAfterUnequip(IItem equippedWeapon, IItem incomingItem)
        {
            _owner.Mediator.Publish(new ProfileSetBoolAction(ProfileConstants.CombatSettingsSpecialAttack, false)); // reset the special bar
            var attackStyle = _owner.Profile.GetValue<int>(ProfileConstants.CombatSettingsAttackStyleOptionId);
            if (attackStyle < equippedWeapon.EquipmentDefinition.AttackStyleIDs.Length &&
                equippedWeapon.EquipmentDefinition.AttackStyleIDs[attackStyle] == AttackStyle.MeleeDefensive)
            {
                for (var styleId = 0; styleId < 4; styleId++)
                {
                    var style = incomingItem.EquipmentDefinition.AttackStyleIDs[styleId];
                    if (style == AttackStyle.MeleeDefensive || style == AttackStyle.RangedLongRange)
                    {
                        _owner.Mediator.Publish(new ProfileSetIntAction(ProfileConstants.CombatSettingsAttackStyleOptionId, styleId));
                        break;
                    }
                }
            }
        }

        /// <summary>Restores an already-equipped item without running equip lifecycle callbacks.</summary>
        public bool TryRestoreEquippedItem(EquipmentSlot slot, IItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!_storage.TryAdd((int)slot, item, out var slots)) return false;
            PublishChanges(slots.Select(slot => (EquipmentSlot)slot).ToHashSet());
            return true;
        }

        private bool MoveFromInventoryToSlot(IItem item, int inventorySlot, EquipmentSlot equipmentSlot) =>
            ItemContainerTransaction.ResolveSingleBoundary(_owner.Inventory.Items)
                .TryTransferTo(_mutations, item, item.Count, inventorySlot, (int)equipmentSlot);

        public bool TryMoveTo(IItemContainer destination, IItem item, int count, EquipmentSlot slot,
            IItem? destinationItem = null)
        {
            if (count <= 0 || this[slot] is not { } equippedItem || !ReferenceEquals(equippedItem, item)) return false;
            var destinationBoundary = ItemContainerTransaction.ResolveSingleBoundary(destination);
            var fullyRemoved = count == equippedItem.Count;
            using var transaction = ItemContainerTransaction.Begin(this, destination);
            if (!_mutations.TryTransferTo(destinationBoundary, equippedItem, count, (int)slot, -1, destinationItem)) return false;
            if (fullyRemoved) DeferEquipmentEffects(new EquipmentEffect(EquipmentEffectKind.Unequipped, equippedItem));
            transaction.Commit();
            return true;
        }

        public bool TryReplaceEquippedItem(EquipmentSlot slot, IItem expectedItem, IItem replacement)
        {
            ArgumentNullException.ThrowIfNull(expectedItem);
            ArgumentNullException.ThrowIfNull(replacement);
            var itemSlot = (int)slot;
            if (!Enum.IsDefined(slot) || (uint)itemSlot >= (uint)_storage.Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            _storage.EnsureMutationAccess();
            lock (_storage.MutationLock)
            {
                if (!ReferenceEquals(_storage[itemSlot], expectedItem)) return false;
                _storage.Replace(itemSlot, replacement);
            }

            CompleteEquipmentChange([slot],
                new EquipmentEffect(EquipmentEffectKind.Unequipped, expectedItem),
                new EquipmentEffect(EquipmentEffectKind.Equipped, replacement));
            return true;
        }

        public int RemoveEquippedItem(IItem item, EquipmentSlot preferredSlot = EquipmentSlot.NoSlot)
        {
            if (preferredSlot == EquipmentSlot.NoSlot)
            {
                preferredSlot = GetInstanceSlot(item);
            }
            var preferredSlotIndex = (int)preferredSlot;
            IItem? equippedItem = null;
            int removed;
            HashSet<int> changedSlots;
            bool fullyRemoved;
            _storage.EnsureMutationAccess();
            lock (_storage.MutationLock)
            {
                if ((uint)preferredSlotIndex < (uint)_storage.Capacity)
                {
                    equippedItem = _storage[preferredSlotIndex];
                }

                removed = _storage.Remove(item, preferredSlotIndex, out changedSlots);
                fullyRemoved = equippedItem != null &&
                               !ReferenceEquals(equippedItem, _storage[preferredSlotIndex]);
            }
            if (removed <= 0)
            {
                return removed;
            }

            var equipmentChanges = changedSlots.Select(changedSlot => (EquipmentSlot)changedSlot).ToHashSet();
            if (!fullyRemoved)
            {
                PublishChanges(equipmentChanges);
                return removed;
            }

            CompleteEquipmentChange(equipmentChanges,
                new EquipmentEffect(EquipmentEffectKind.Unequipped, equippedItem!));
            return removed;
        }

        public void ClearEquipment()
        {
            IItem[] equippedItems;
            bool cleared;
            _storage.EnsureMutationAccess();
            lock (_storage.MutationLock)
            {
                equippedItems = _storage.ToArray().Where(item => item != null).Cast<IItem>().ToArray();
                cleared = _storage.Clear();
            }
            if (!cleared) return;

            var effects = new EquipmentEffect[equippedItems.Length];
            for (var index = 0; index < equippedItems.Length; index++)
                effects[index] = new EquipmentEffect(EquipmentEffectKind.Unequipped, equippedItems[index]);
            CompleteEquipmentChange(null, effects);
        }

        private void CompleteEquipmentChange(HashSet<EquipmentSlot>? slots, params EquipmentEffect[] effects)
        {
            if (_storage.Transaction != null)
            {
                DeferEquipmentEffects(effects);
                PublishChanges(slots);
                return;
            }

            var failures = ExecuteEquipmentEffects(effects);
            try { PublishChanges(slots); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
            ThrowEquipmentFailures(failures);
        }

        // Equipment owns these small lifecycle batches; the transaction cannot schedule arbitrary work.
        private void DeferEquipmentEffects(params EquipmentEffect[] effects)
        {
            var transaction = _storage.Transaction
                ?? throw new InvalidOperationException("Equipment effects require an active item-container transaction.");
            transaction.EnsureActive();
            var pending = _pendingCompletion.GetOrAdd(transaction, _ => new Queue<EquipmentCompletion>());
            pending.Enqueue(new EquipmentCompletion(transaction.NextCompletionOrder(), effects));
        }

        void IItemContainerCompletionOwner.DiscardPendingCompletion(ItemContainerTransaction transaction) => _pendingCompletion.TryRemove(transaction, out _);
        void IItemContainerCompletionOwner.CompleteAfterPublication(ItemContainerTransaction transaction, int order) { }
        void IItemContainerCompletionOwner.CompleteBeforePublication(ItemContainerTransaction transaction, int order)
        {
            if (!_pendingCompletion.TryGetValue(transaction, out var pending) || !pending.TryPeek(out var completion) || completion.Order != order) return;
            pending.Dequeue();
            ThrowEquipmentFailures(ExecuteEquipmentEffects(completion.Effects));
        }

        private void ExecuteEquipmentEffect(EquipmentEffect effect)
        {
            switch (effect.Kind)
            {
                case EquipmentEffectKind.Equipped: effect.Item.EquipmentScript.OnEquipped(effect.Item, _owner); break;
                case EquipmentEffectKind.Unequipped: effect.Item.EquipmentScript.OnUnequipped(effect.Item, _owner); break;
                case EquipmentEffectKind.WeaponProfile: UpdateWeaponProfileAfterUnequip(effect.Item, effect.Incoming!); break;
                default: throw new ArgumentOutOfRangeException(nameof(effect), effect.Kind, "Unknown equipment effect.");
            }
        }

        private List<Exception>? ExecuteEquipmentEffects(EquipmentEffect[] effects)
        {
            List<Exception>? exceptions = null;
            foreach (var effect in effects)
            {
                try
                {
                    ExecuteEquipmentEffect(effect);
                }
                catch (Exception exception)
                {
                    (exceptions ??= []).Add(exception);
                }
            }

            return exceptions;
        }

        private static void ThrowEquipmentFailures(List<Exception>? exceptions)
        {
            if (exceptions is { Count: 1 })
            {
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
            }
            else if (exceptions is { Count: > 1 })
            {
                var failures = new List<Exception>();
                foreach (var exception in exceptions)
                {
                    if (exception is AggregateException aggregate) failures.AddRange(aggregate.Flatten().InnerExceptions);
                    else failures.Add(exception);
                }
                throw new AggregateException("Multiple post-commit equipment actions failed.", failures);
            }
        }

        private void PublishChanges(HashSet<EquipmentSlot>? slots = null) =>
            _mutations.NotifyChanges(slots?.Select(slot => (int)slot).ToHashSet());

        private void PublishCommittedChanges(HashSet<EquipmentSlot>? slots)
        {
            _owner.Appearance.DrawCharacter();
            _owner.Statistics.CalculateBonuses();
            _owner.EventManager.SendEvent(new EquipmentChangedEvent(_owner, slots));
        }

        /// <summary>
        /// UnEquips item to this character.
        /// </summary>
        /// <remarks>Script eligibility checks remain synchronous domain validation and can have their own side effects.</remarks>
        /// <param name="item">The item.</param>
        /// <param name="toInventorySlot">To inventory slot.</param>
        /// <returns>True if item was unequipped sucessfully.</returns>
        public bool UnEquipItem(IItem item, int toInventorySlot = -1)
        {
            var slot = GetInstanceSlot(item);
            if (slot == EquipmentSlot.NoSlot)
            {
                return false;
            }

            if (!item.EquipmentScript.CanUnEquipItem(item, _owner))
            {
                return false;
            }
            var destinationSlot = -1;
            if (toInventorySlot >= 0 && (uint)toInventorySlot < (uint)_owner.Inventory.Items.Capacity &&
                _owner.Inventory.Items[toInventorySlot] == null)
            {
                destinationSlot = toInventorySlot;
            }

            var inventoryBoundary = ItemContainerTransaction.ResolveSingleBoundary(_owner.Inventory.Items);
            using (var transaction = ItemContainerTransaction.Begin(_owner.Inventory.Items, this))
            {
                if (_mutations.TryTransferTo(inventoryBoundary, item, item.Count, (int)slot, destinationSlot))
                {
                    DeferEquipmentEffects(new EquipmentEffect(EquipmentEffectKind.Unequipped, item));
                    transaction.Commit();
                    return true;
                }
            }
            _owner.SendChatMessage("Not enough space in your inventory.");
            return false;
        }

        public EquipmentSlot GetInstanceSlot(IItem instance) => (EquipmentSlot)_storage.GetInstanceSlot(instance);
        public void Hydrate(IReadOnlyList<HydratedItemDto> equipment)
        {
            var items = equipment.Select(entry => entry.ToStorageEntry(_itemBuilder)).ToArray();
            _storage.RestoreItems(items);
            _owner.Statistics.CalculateBonuses();
            foreach (var (_, item) in items) item.EquipmentScript.OnEquipped(item, _owner);
        }

        public IReadOnlyList<HydratedItemDto> Dehydrate() => _storage.Select((item, slot) => (item, slot)).Where(x => x.item != null)
            .Select(entry => new HydratedItemDto(entry.item!.Id, entry.item.Count, entry.slot, entry.item.SerializeExtraData()))
            .ToArray();
    }
}
