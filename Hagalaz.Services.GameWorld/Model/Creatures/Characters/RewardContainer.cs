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
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public class RewardContainer : BaseItemContainer, IRewardContainer, IHydratable<IReadOnlyList<HydratedItemDto>>,
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
        public RewardContainer(ICharacter owner)
            : base(StorageType.AlwaysStack, byte.MaxValue)
        {
            _owner = owner;
            _itemBuilder = owner.ServiceProvider.GetRequiredService<IItemBuilder>();
        }

        /// <summary>
        /// Withdraws from reward container.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public int Claim(IItem item, int count)
        {
            var slot = GetInstanceSlot(item);
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

            var removed = Remove(toRemove, slot);
            if (removed <= 0)
            {
                return -1;
            }

            toRemove.Count = removed;
            if (!_owner.Inventory.Add(toRemove))
            {
                return -1;
            }

            Sort();
            return removed;
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public override void OnUpdate(HashSet<int>? slots = null) => new RewardsChangedEvent(_owner, slots).Send();

        public void Hydrate(IReadOnlyList<HydratedItemDto> rewards)
        {
            var items = new IItem[Capacity];
            foreach (var reward in rewards)
            {
                var item = _itemBuilder.Create().WithId(reward.ItemId).WithCount(reward.Count);
                if (!string.IsNullOrEmpty(reward.ExtraData))
                {
                    item = item.WithExtraData(reward.ExtraData);
                }

                items[reward.SlotId] = item.Build();
            }

            SetItems(items, false);
        }

        public IReadOnlyList<HydratedItemDto> Dehydrate()
        {
            var items = ToArray();
            return items
                .Where(item => item != null)
                .Select((item, index) => new HydratedItemDto(item!.Id, item!.Count, index, item!.SerializeExtraData()))
                .ToList();
        }
    }
}