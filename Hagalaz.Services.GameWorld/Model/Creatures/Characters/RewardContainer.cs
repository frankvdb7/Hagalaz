using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Resources;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public partial class RewardContainer : IRewardContainer, IItemContainerStorageProvider, IHydratable<IReadOnlyList<HydratedItemDto>>,
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
        private readonly ItemContainerStorage _storage;

        ItemContainerStorage IItemContainerStorageProvider.Storage => _storage;

        public RewardContainer(ICharacter owner, IItemBuilder itemBuilder)
        {
            _owner = owner;
            _itemBuilder = itemBuilder;
            _storage = new ItemContainerStorage(StorageType.AlwaysStack, byte.MaxValue);
        }

        /// <summary>
        /// Withdraws from reward container.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public int Claim(IItem item, int count)
        {
            var slot = _storage.GetInstanceSlot(item);
            if (slot == -1 || count <= 0) return -1;
            var toRemove = item.Clone();
            if (toRemove.Count < count) count = toRemove.Count;
            toRemove.Count = count;

            var stack = toRemove.ItemDefinition.Stackable || toRemove.ItemDefinition.Noted;
            var needSlots = 0;
            if (stack)
            {
                if (_owner.Inventory.GetSlotByItem(toRemove) != -1)
                {
                    var total = _owner.Inventory.GetCount(toRemove) + (long)count;
                    if (total > int.MaxValue)
                    {
                        return -1;
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
                    return -1;
                }

                count = freeSlots;
                toRemove.Count = count;
            }

            if (!ItemContainerTransfer.TryTransfer(this, _owner.Inventory, item, count, slot))
            {
                return -1;
            }

            Sort();
            return count;
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public void OnUpdate(HashSet<int>? slots = null) => _owner.EventManager.SendEvent(new RewardsChangedEvent(_owner, slots));

        public void Hydrate(IReadOnlyList<HydratedItemDto> rewards)
        {
            var items = new IItem?[Capacity];
            foreach (var entry in rewards)
            {
                if ((uint)entry.SlotId >= (uint)Capacity || entry.Count <= 0 || items[entry.SlotId] != null)
                    throw new ArgumentException("Rewards contain an invalid restored slot.", nameof(rewards));
                items[entry.SlotId] = _itemBuilder.Create().WithId(entry.ItemId).WithCount(entry.Count)
                    .WithExtraData(entry.ExtraData ?? string.Empty).Build();
            }
            _storage.ReplaceState(items);
        }

        public IReadOnlyList<HydratedItemDto> Dehydrate()
        {
            var entries = _storage.Select((item, slot) => (item, slot)).Where(x => x.item != null).ToArray();
            return entries.Select(entry => new HydratedItemDto(entry.item!.Id, entry.item.Count, entry.slot,
                entry.item.SerializeExtraData())).ToArray();
        }
    }
}
