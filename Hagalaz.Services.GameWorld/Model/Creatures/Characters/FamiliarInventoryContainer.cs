using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public class FamiliarInventoryContainer : BaseItemContainer, IFamiliarInventoryContainer, IHydratable<IReadOnlyList<HydratedItem>>, IDehydratable<IReadOnlyList<HydratedItem>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// Constructs a container for character inventories.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        /// <param name="type">The type of container.</param>
        /// <param name="capacity">The capacity of the container.</param>
        public FamiliarInventoryContainer(ICharacter owner, StorageType type, int capacity)
            : base(type, capacity) =>
            _owner = owner;

        /// <summary>
        /// Deposit's specific item into familiars inventory.
        /// </summary>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// If depositing was successful.
        /// </returns>
        public bool DepositFromInventory(IItem item, int count)
        {
            var slot = _owner.Inventory.GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
                return false;
            var toRemove = item.Clone();
            toRemove.Count = count;

            if (!HasSpaceFor(toRemove))
            {
                _owner.SendChatMessage(GameStrings.FamiliarInventoryFull);
                return false;
            }

            var removed = _owner.Inventory.Remove(toRemove, slot);
            if (removed > 0)
            {
                toRemove.Count = removed;
                if (!Add(toRemove))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Withdraws from familiar inventory.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public bool WithdrawFromFamiliarInventory(IItem item, int count)
        {
            var slot = GetInstanceSlot(item);
            if (slot == -1 || count <= 0)
                return false;
            var toRemove = item.Clone();
            toRemove.Count = count;
            if (!_owner.Inventory.HasSpaceFor(toRemove))
            {
                _owner.SendChatMessage(GameStrings.InventoryFull);
                return false;
            }

            var removed = Remove(toRemove, slot);
            if (removed > 0)
            {
                toRemove.Count = removed;
                if (!_owner.Inventory.Add(toRemove))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public override void OnUpdate(HashSet<int>? slots = null) => _owner.EventManager.SendEvent(new FamiliarInventoryChangedEvent(_owner, slots));

        public void Hydrate(IReadOnlyList<HydratedItem> inventory)
        {
            var items = new IItem[Capacity];
            var itemBuilder = _owner.ServiceProvider.GetRequiredService<IItemBuilder>();
            foreach (var hydrated in inventory)
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

        public IReadOnlyList<HydratedItem> Dehydrate()
        {
            var items = ToArray();
            return items
                .OfType<IItem>()
                .Select((item, index) => new HydratedItem(item!.Id, item!.Count, index, item!.SerializeExtraData()))
                .ToList();
        }
    }
}