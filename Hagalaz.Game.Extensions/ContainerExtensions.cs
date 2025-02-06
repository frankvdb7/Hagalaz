using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Extensions
{
    public static class ContainerExtensions
    {
        /// <summary>
        /// Adds the generated loot to the inventory.
        /// If there is no space, the loot is dropped instead.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="character"></param>
        /// <param name="table"></param>
        /// <param name="addedItems"></param>
        public static void TryAddLoot(this IInventoryContainer container, ICharacter character, ILootTable table, out IEnumerable<IItem> addedItems)
        {
            var lootGenerator = character.ServiceProvider.GetRequiredService<ILootGenerator>();
            TryAddLoot(container, character, lootGenerator.GenerateLoot<ILootItem>(new CharacterLootParams(table, character)), out addedItems);
        }

        /// <summary>
        /// Adds the loot to inventory.
        /// If there is no space, the loot is dropped instead.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="character">The character.</param>
        /// <param name="lootResults">The loot results.</param>
        /// <param name="addedItems"></param>
        public static void TryAddLoot(
            this IInventoryContainer container, ICharacter character, IEnumerable<LootResult<ILootItem>> lootResults, out IEnumerable<IItem> addedItems) =>
            TryAddItems(container,
                character,
                lootResults.Select(result => (result.Item.Id, result.Count)),
                out addedItems);

        /// <summary>
        /// Adds the items to the characters inventory.
        /// If there is no space, the item is dropped instead.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="character"></param>
        /// <param name="items"></param>
        /// <param name="addedItems"></param>
        public static void TryAddItems(
            this IInventoryContainer container, ICharacter character, IEnumerable<(int ItemId, int ItemCount)> items, out IEnumerable<IItem> addedItems)
        {
            var itemBuilder = character.ServiceProvider.GetRequiredService<IItemBuilder>();
            TryAddItems(container, character, items.Select(item => itemBuilder.Create().WithId(item.ItemId).WithCount(item.ItemCount).Build()), out addedItems);
        }

        /// <summary>
        /// Adds the item to characters inventory.
        /// If there is no space, the item is dropped instead.
        /// </summary>
        /// <returns></returns>
        public static void TryAddItems(this IInventoryContainer container, ICharacter character, IEnumerable<IItem> items, out IEnumerable<IItem> addedItems)
        {
            var groundItemBuilder = character.ServiceProvider.GetRequiredService<IGroundItemBuilder>();
            var added = new List<IItem>();
            foreach (var item in items)
            {
                if (!container.Add(item))
                {
                    groundItemBuilder.Create().WithItem(item).WithLocation(character.Location).WithOwner(character).Spawn();
                }

                added.Add(item);
            }

            addedItems = added;
        }
    }
}