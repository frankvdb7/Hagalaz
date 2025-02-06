using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Class InventoryContainer
    /// </summary>
    public class InventoryContainer : BaseItemContainer, IInventoryContainer, IHydratable<IReadOnlyList<HydratedItemDto>>,
        IDehydratable<IReadOnlyList<HydratedItemDto>>
    {
        /// <summary>
        /// Instance of the character who owns this container.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// Contstructs a container for character inventories.
        /// </summary>
        /// <param name="owner">The owner of the container.</param>
        /// <param name="capacity">The capacity of the container.</param>
        public InventoryContainer(ICharacter owner, int capacity)
            : base(StorageType.Normal, capacity) =>
            _owner = owner;

        /// <summary>
        /// Called when multiple items from specified slot(s) have changed.
        /// </summary>
        /// <param name="slots">The slots.</param>
        public override void OnUpdate(HashSet<int>? slots = null) => new InventoryChangedEvent(_owner, slots).Send();

        /// <summary>
        /// Drop's specific item.
        /// </summary>
        /// <param name="item">Item which should be droped.</param>
        /// <returns>If item was droped successfully.</returns>
        public bool DropItem(IItem item)
        {
            var slot = GetInstanceSlot(item);
            if (slot == -1) return false;
            if (Remove(item, slot) < item.Count) return false;
            var groundItemBuilder = _owner.ServiceProvider.GetRequiredService<IGroundItemBuilder>();
            var groundItem = groundItemBuilder.Create()
                .WithItem(item)
                .WithLocation(_owner.Location)
                .WithOwner(_owner)
                .Build();
            _owner.Region.Add(groundItem); // we spawn it with this method, as the container was normally stacked.
            return true;
        }

        public void Hydrate(IReadOnlyList<HydratedItemDto> inventory)
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

        public IReadOnlyList<HydratedItemDto> Dehydrate()
        {
            var items = ToArray();
            return items
                .OfType<IItem>()
                .Select((item, index) => new HydratedItemDto(item.Id, item.Count, index, item!.SerializeExtraData()))
                .ToList();
        }
    }
}