using System;
using System.Collections.Generic;
using System.Linq;
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
    public partial class EquipmentContainer : IEquipmentContainer, IItemContainerStorageOwner, IHydratable<IReadOnlyList<HydratedItemDto>>,
        IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;
        private readonly IItemBuilder _itemBuilder;
        private readonly ItemContainerStorage _storage;
        ItemContainerStorage IItemContainerStorageOwner.Storage => _storage;
        void IItemContainerStorageOwner.PublishChanges(HashSet<int>? changedSlots) =>
            PublishChanges(changedSlots?.Select(slot => (EquipmentSlot)slot).ToHashSet());
        public int Capacity => _storage.Capacity;
        public int FreeSlots => _storage.FreeSlots;
        public IItem? this[int index] => _storage[index];
        public System.Collections.Generic.IEnumerator<IItem?> GetEnumerator() => _storage.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

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
        }

        /// <summary>
        /// Equips item to this character.
        /// </summary>
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

                if (!TryMoveFromInventoryToSlot(item, slot, equipSlot, out var inventorySlots, out var equipmentSlots))
                {
                    return false;
                }

                PublishEquipmentMove(inventorySlots, equipmentSlots);
                return true;
            }

            if (equipSlot != EquipmentSlot.Weapon && equipSlot != EquipmentSlot.Shield)
            {
                if (equipItem == null)
                {
                    if (!TryMoveFromInventoryToSlot(item, slot, equipSlot, out var inventorySlots, out var equipmentSlots))
                    {
                        return false;
                    }

                    item.EquipmentScript.OnEquipped(item, _owner);
                    PublishEquipmentMove(inventorySlots, equipmentSlots);
                    return true;
                }

            if (_owner.Inventory.Items.Remove(item, slot) <= 0)
                {
                    return false;
                }

                if (!equipItem.EquipmentScript.UnEquipItem(equipItem, _owner, slot))
                {
                    _owner.Inventory.Items.Add(slot, item);
                    return false;
                }

                Add(equipSlot, item);
                item.EquipmentScript.OnEquipped(item, _owner);
                return true;
            }

            var equippedWeapon = this[EquipmentSlot.Weapon];
            var equippedShield = this[EquipmentSlot.Shield];
            if (equippedWeapon == null && equippedShield == null)
            {
                if (!TryMoveFromInventoryToSlot(item, slot, equipSlot, out var inventorySlots, out var equipmentSlots))
                {
                    return false;
                }

                item.EquipmentScript.OnEquipped(item, _owner);
                PublishEquipmentMove(inventorySlots, equipmentSlots);
                return true;
            }

            if (_owner.Inventory.Items.Remove(item, slot) <= 0)
            {
                return false;
            }

            var needsWeaponUnequip = equippedWeapon != null && (equipSlot == EquipmentSlot.Weapon ||
                                                                equipSlot == EquipmentSlot.Shield &&
                                                                equippedWeapon.EquipmentDefinition.Type == EquipmentType.TwoHanded);
            var needsShieldUnequip = equipSlot == EquipmentSlot.Shield
                ? equippedShield != null
                : equippedShield != null && item.EquipmentDefinition.Type == EquipmentType.TwoHanded;
            var needsFreeSlots = 0;
            if (needsWeaponUnequip)
            {
                if (!_owner.Inventory.Items.HasSpaceFor(equippedWeapon!))
                {
                    _owner.SendChatMessage("Not enough space in your inventory.");
                    _owner.Inventory.Items.Add(slot, item);
                    return false;
                }

                if (!equippedWeapon!.ItemDefinition.Stackable && !equippedWeapon.ItemDefinition.Noted && _owner.Inventory.Items.Type != StorageType.AlwaysStack)
                    needsFreeSlots++;
            }

            if (needsShieldUnequip)
            {
                if (!_owner.Inventory.Items.HasSpaceFor(equippedShield!))
                {
                    _owner.SendChatMessage("Not enough space in your inventory.");
                    _owner.Inventory.Items.Add(slot, item);
                    return false;
                }

                if (!equippedShield!.ItemDefinition.Stackable && !equippedShield.ItemDefinition.Noted && _owner.Inventory.Items.Type != StorageType.AlwaysStack)
                    needsFreeSlots++;
            }

            if (_owner.Inventory.Items.FreeSlots < needsFreeSlots)
            {
                _owner.SendChatMessage("Not enough space in your inventory.");
                _owner.Inventory.Items.Add(slot, item);
                return false;
            }

            if (needsWeaponUnequip)
            {
                if (!equippedWeapon!.EquipmentScript.UnEquipItem(equippedWeapon, _owner, slot))
                {
                    _owner.SendChatMessage("System error. [" + equippedWeapon.Id + "," + equippedWeapon.Count + "]");
                    _owner.Inventory.Items.Add(slot, item);
                    return false;
                }
            }

            if (needsShieldUnequip)
            {
                if (!equippedShield!.EquipmentScript.UnEquipItem(equippedShield, _owner, slot))
                {
                    _owner.SendChatMessage("System error. [" + equippedShield.Id + "," + equippedShield.Count + "]");
                    _owner.Inventory.Items.Add(slot, item);
                    return false;
                }
            }

            if (needsWeaponUnequip)
            {
                _owner.Mediator.Publish(new ProfileSetBoolAction(ProfileConstants.CombatSettingsSpecialAttack, false)); // reset the special bar
                if (_owner.Profile.GetValue<int>(ProfileConstants.CombatSettingsAttackStyleOptionId) <
                    equippedWeapon!.EquipmentDefinition.AttackStyleIDs.Length &&
                    equippedWeapon.EquipmentDefinition.AttackStyleIDs[_owner.Profile.GetValue<int>(ProfileConstants.CombatSettingsAttackStyleOptionId)] ==
                    AttackStyle.MeleeDefensive)
                {
                    for (var styleId = 0; styleId < 4; styleId++)
                    {
                        var style = item.EquipmentDefinition.AttackStyleIDs[styleId];
                        if (style == AttackStyle.MeleeDefensive || style == AttackStyle.RangedLongRange)
                        {
                            _owner.Mediator.Publish(new ProfileSetIntAction(ProfileConstants.CombatSettingsAttackStyleOptionId, styleId));
                            break;
                        }
                    }
                }
            }

            Add(equipSlot, item);
            item.EquipmentScript.OnEquipped(item, _owner);
            return true;
        }

        public bool Add(EquipmentSlot slot, IItem item)
        {
            if (!_storage.TryAdd((int)slot, item, out var slots)) return false;
            PublishChanges(slots?.Select(changedSlot => (EquipmentSlot)changedSlot).ToHashSet());
            return true;
        }

        private bool TryMoveFromInventoryToSlot(IItem item, int inventorySlot, EquipmentSlot equipmentSlot,
            out HashSet<int> inventorySlots, out HashSet<int> equipmentSlots) =>
            ItemContainerTransfer.TryTransferStorage((IItemContainerStorageOwner)_owner.Inventory.Items, this, item, item.Count, inventorySlot, (int)equipmentSlot, null,
                out inventorySlots, out equipmentSlots);

        private void PublishEquipmentMove(HashSet<int> inventorySlots, HashSet<int> equipmentSlots)
        {
            ((IItemContainerStorageOwner)_owner.Inventory.Items).PublishChanges(inventorySlots);
            PublishChanges(equipmentSlots?.Select(slot => (EquipmentSlot)slot).ToHashSet());
        }

        public void Replace(EquipmentSlot slot, IItem item)
        {
            var itemSlot = (int)slot;
            var oldItem = _storage[itemSlot];
            _storage.Replace(itemSlot, item);
            PublishChanges([slot]);
            oldItem?.EquipmentScript.OnUnequipped(oldItem, _owner);
            item.EquipmentScript.OnEquipped(item, _owner);
        }

        public int Remove(IItem item, EquipmentSlot preferredSlot = EquipmentSlot.NoSlot, bool update = true)
        {
            if (preferredSlot == EquipmentSlot.NoSlot)
            {
                preferredSlot = GetInstanceSlot(item);
            }
            var preferredSlotIndex = (int)preferredSlot;
            var removed = _storage.Remove(item, preferredSlotIndex, out var changedSlots);
            if (removed > 0 && update) PublishChanges(changedSlots?.Select(changedSlot => (EquipmentSlot)changedSlot).ToHashSet());
            if (removed <= 0)
            {
                return removed;
            }

            if (removed != item.Count)
            {
                return removed;
            }

            item.EquipmentScript.OnUnequipped(item, _owner);
            return removed;
        }

        public void Clear(bool update)
        {
            foreach (var item in _storage.ToArray())
            {
                item?.EquipmentScript.OnUnequipped(item, _owner);
            }
            if (_storage.Clear() && update) PublishChanges(null);
        }

        private void PublishChanges(HashSet<EquipmentSlot>? slots = null)
        {
            _owner.Appearance.DrawCharacter();
            _owner.Statistics.CalculateBonuses();
            _owner.EventManager.SendEvent(new EquipmentChangedEvent(_owner, slots));
        }

        /// <summary>
        /// UnEquips item to this character.
        /// </summary>
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

            if (!ItemContainerTransfer.TryTransferStorage(this, (IItemContainerStorageOwner)_owner.Inventory.Items, item, item.Count, (int)slot, destinationSlot, null,
                    out var equipmentSlots, out var inventorySlots))
            {
                _owner.SendChatMessage("Not enough space in your inventory.");
                return false;
            }

            item.EquipmentScript.OnUnequipped(item, _owner);
            PublishEquipmentMove(inventorySlots, equipmentSlots);
            return true;
        }

        public EquipmentSlot GetInstanceSlot(IItem instance) => (EquipmentSlot)_storage.GetInstanceSlot(instance);
        public IItem? GetById(int id) => _storage.GetById(id);

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
