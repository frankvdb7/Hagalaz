using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Game.Abstractions.Builders.Item;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="IItemContainer"/> and its derived interfaces,
    /// simplifying common container operations like adding loot and items.
    /// </summary>
    public static class ContainerExtensions
    {
        /// <summary>
        /// Generates loot from a specified loot table and attempts to add it to the character's inventory.
        /// If the inventory is full, any items that cannot be added are dropped on the ground at the character's location.
        /// </summary>
        /// <param name="container">The inventory container to add the loot to.</param>
        /// <param name="character">The character receiving the loot.</param>
        /// <param name="table">The loot table to generate items from.</param>
        /// <param name="addedItems">When this method returns, contains a collection of the items that were successfully generated and either added to inventory or dropped.</param>
        public static void TryAddLoot(this IInventoryContainer container, ICharacter character, ILootTable table, out IEnumerable<IItem> addedItems)
        {
            var lootGenerator = character.ServiceProvider.GetRequiredService<ILootGenerator>();
            TryAddLoot(container, character, lootGenerator.GenerateLoot<ILootItem>(new CharacterLootParams(table, character)), out addedItems);
        }

        /// <summary>
        /// Attempts to add a collection of pre-generated loot results to the character's inventory.
        /// If the inventory is full, any items that cannot be added are dropped on the ground at the character's location.
        /// </summary>
        /// <param name="container">The inventory container to add the loot to.</param>
        /// <param name="character">The character receiving the loot.</param>
        /// <param name="lootResults">The collection of loot results to be added.</param>
        /// <param name="addedItems">When this method returns, contains a collection of the items that were successfully processed and either added to inventory or dropped.</param>
        public static void TryAddLoot(
            this IInventoryContainer container, ICharacter character, IEnumerable<LootResult<ILootItem>> lootResults, out IEnumerable<IItem> addedItems) =>
            TryAddItems(container,
                character,
                lootResults.Select(result => (result.Item.Id, result.Count)),
                out addedItems);

        /// <summary>
        /// Attempts to add a collection of items, specified by ID and count, to the character's inventory.
        /// If the inventory is full, any items that cannot be added are dropped on the ground at the character's location.
        /// </summary>
        /// <param name="container">The inventory container to add the items to.</param>
        /// <param name="character">The character receiving the items.</param>
        /// <param name="items">A collection of tuples, where each tuple contains an item ID and the desired count.</param>
        /// <param name="addedItems">When this method returns, contains a collection of the item instances that were created and either added to inventory or dropped.</param>
        public static void TryAddItems(
            this IInventoryContainer container, ICharacter character, IEnumerable<(int ItemId, int ItemCount)> items, out IEnumerable<IItem> addedItems)
        {
            var itemBuilder = character.ServiceProvider.GetRequiredService<IItemBuilder>();
            TryAddItems(container, character, items.Select(item => itemBuilder.Create().WithId(item.ItemId).WithCount(item.ItemCount).Build()), out addedItems);
        }

        /// <summary>
        /// Attempts to add a collection of item instances to the character's inventory.
        /// If the inventory is full, any items that cannot be added are dropped on the ground at the character's location.
        /// </summary>
        /// <param name="container">The inventory container to add the items to.</param>
        /// <param name="character">The character receiving the items.</param>
        /// <param name="items">The collection of item instances to add.</param>
        /// <param name="addedItems">When this method returns, contains the collection of items that were processed.</param>
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