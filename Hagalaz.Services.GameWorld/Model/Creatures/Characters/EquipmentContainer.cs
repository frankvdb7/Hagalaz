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
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Class EquipmentContainer
    /// </summary>
    public class EquipmentContainer : BaseItemContainer, IEquipmentContainer, IHydratable<IReadOnlyList<HydratedItemDto>>,
        IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// Gets the item by the specified array index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>Returns the Item object.</returns>
        public IItem? this[EquipmentSlot index] => Items[(int)index];

        /// <summary>
        /// Constructs a container for character equipment.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        /// <param name="capacity">The capacity of the container.</param>
        public EquipmentContainer(ICharacter owner, int capacity)
            : base(StorageType.Normal, capacity) =>
            _owner = owner;

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public override void OnUpdate(HashSet<int>? slots = null) => OnUpdate(slots?.Select(s => (EquipmentSlot)s).ToHashSet());

        /// <summary>
        /// Equips item to this character.
        /// </summary>
        /// <param name="item">Item in inventory.</param>
        /// <returns>True if item was equiped sucessfully.</returns>
        public bool EquipItem(IItem item)
        {
            var slot = _owner.Inventory.GetInstanceSlot(item);
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

                if (_owner.Inventory.Remove(item, slot) <= 0)
                {
                    return false;
                }

                Add(equipSlot, item);
                return true;
            }

            if (equipSlot != EquipmentSlot.Weapon && equipSlot != EquipmentSlot.Shield)
            {
                if (_owner.Inventory.Remove(item, slot) <= 0)
                {
                    return false;
                }

                if (equipItem != null)
                {
                    if (!equipItem.EquipmentScript.UnEquipItem(equipItem, _owner, slot))
                    {
                        _owner.Inventory.Add(slot, item);
                        return false;
                    }
                }

                Add(equipSlot, item);
                item.EquipmentScript.OnEquiped(item, _owner);
                return true;
            }

            var equippedWeapon = this[EquipmentSlot.Weapon];
            var equippedShield = this[EquipmentSlot.Shield];
            if (equippedWeapon == null && equippedShield == null)
            {
                if (_owner.Inventory.Remove(item, slot) <= 0)
                {
                    return false;
                }

                Add(equipSlot, item);
                item.EquipmentScript.OnEquiped(item, _owner);
                return true;
            }

            if (_owner.Inventory.Remove(item, slot) <= 0)
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
                if (!_owner.Inventory.HasSpaceFor(equippedWeapon!))
                {
                    _owner.SendChatMessage("Not enough space in your inventory.");
                    _owner.Inventory.Add(slot, item);
                    return false;
                }

                if (!equippedWeapon!.ItemDefinition.Stackable && !equippedWeapon.ItemDefinition.Noted && _owner.Inventory.Type != StorageType.AlwaysStack)
                    needsFreeSlots++;
            }

            if (needsShieldUnequip)
            {
                if (!_owner.Inventory.HasSpaceFor(equippedShield!))
                {
                    _owner.SendChatMessage("Not enough space in your inventory.");
                    _owner.Inventory.Add(slot, item);
                    return false;
                }

                if (!equippedShield!.ItemDefinition.Stackable && !equippedShield.ItemDefinition.Noted && _owner.Inventory.Type != StorageType.AlwaysStack)
                    needsFreeSlots++;
            }

            if (_owner.Inventory.FreeSlots < needsFreeSlots)
            {
                _owner.SendChatMessage("Not enough space in your inventory.");
                _owner.Inventory.Add(slot, item);
                return false;
            }

            if (needsWeaponUnequip)
            {
                if (!equippedWeapon!.EquipmentScript.UnEquipItem(equippedWeapon, _owner, slot))
                {
                    _owner.SendChatMessage("System error. [" + equippedWeapon.Id + "," + equippedWeapon.Count + "]");
                    _owner.Inventory.Add(slot, item);
                    return false;
                }
            }

            if (needsShieldUnequip)
            {
                if (!equippedShield!.EquipmentScript.UnEquipItem(equippedShield, _owner, slot))
                {
                    _owner.SendChatMessage("System error. [" + equippedShield.Id + "," + equippedShield.Count + "]");
                    _owner.Inventory.Add(slot, item);
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
            item.EquipmentScript.OnEquiped(item, _owner);
            return true;
        }

        public bool Add(EquipmentSlot slot, IItem item) => base.Add((int)slot, item);

        public void Replace(EquipmentSlot slot, IItem item)
        {
            var itemSlot = (int)slot;
            var oldItem = Items[itemSlot];
            base.Replace(itemSlot, item);
            oldItem?.EquipmentScript.OnUnequiped(oldItem, _owner);
            item.EquipmentScript.OnEquiped(item, _owner);
        }

        public int Remove(IItem item, EquipmentSlot preferredSlot = EquipmentSlot.NoSlot, bool update = true)
        {
            if (preferredSlot == EquipmentSlot.NoSlot)
            {
                preferredSlot = GetInstanceSlot(item);
            }
            var preferredSlotIndex = (int)preferredSlot;
            var removed = base.Remove(item, preferredSlotIndex, update);
            if (removed <= 0)
            {
                return removed;
            }

            if (removed != item.Count)
            {
                return removed;
            }

            item.EquipmentScript.OnUnequiped(item, _owner);
            return removed;
        }

        public override void Clear(bool update)
        {
            foreach (var item in Items)
            {
                item?.EquipmentScript.OnUnequiped(item, _owner);
            }
            base.Clear(update);
        }

        public void OnUpdate(HashSet<EquipmentSlot>? slots = null)
        {
            _owner.Appearance.DrawCharacter();
            _owner.Statistics.CalculateBonuses();
            new EquipmentChangedEvent(_owner, slots).Send();
        }

        /// <summary>
        /// UnEquips item to this character.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="toInventorySlot">To inventory slot.</param>
        /// <returns>True if item was unequiped sucessfully.</returns>
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
            if (!_owner.Inventory.HasSpaceFor(item))
            {
                _owner.SendChatMessage("Not enough space in your inventory.");
                return false;
            }

            if (base.Remove(item, (int)slot) <= 0)
            {
                return false;
            }
            item.EquipmentScript.OnUnequiped(item, _owner);
            if (toInventorySlot != -1)
            {
                if (_owner.Inventory[toInventorySlot] == null)
                {
                    _owner.Inventory.Add(toInventorySlot, item);
                }
                else
                {
                    _owner.Inventory.Add(item);
                }
            }
            else
            {
                _owner.Inventory.Add(item);
            }

            return true;
        }

        public new EquipmentSlot GetInstanceSlot(IItem instance) => (EquipmentSlot)base.GetInstanceSlot(instance);

        public override void SetItems(IItem[] items, bool update)
        {
            base.SetItems(items, update);
            if (update)
            {
                return;
            }

            _owner.Statistics.CalculateBonuses();
            foreach (var item in items.Where(i => i != null))
            {
                item.EquipmentScript.OnEquiped(item, _owner);
            }
        }

        public void Hydrate(IReadOnlyList<HydratedItemDto> equipment)
        {
            var items = new IItem[Capacity];
            var itemBuilder = _owner.ServiceProvider.GetRequiredService<IItemBuilder>();
            foreach (var hydrated in equipment)
            {
                var builder = itemBuilder.Create().WithId(hydrated.ItemId).WithCount(hydrated.Count);
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