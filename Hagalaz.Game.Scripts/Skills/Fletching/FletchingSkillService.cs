using System;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Dialogues.Generic;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    public class FletchingSkillService : IFletchingSkillService
    {
        private readonly IItemBuilder _itemBuilder;
        private readonly IFletchingService _fletchingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FletchingSkillService"/> class.
        /// </summary>
        /// <param name="itemBuilder">The item builder to create new items.</param>
        /// <param name="fletchingService">The service providing the persisted Fletching recipes.</param>
        public FletchingSkillService(IItemBuilder itemBuilder, IFletchingService fletchingService)
        {
            _itemBuilder = itemBuilder;
            _fletchingService = fletchingService;
        }

        /// <summary>
        ///     Tries the make strung bow.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The item.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public bool TryFletchWood(ICharacter character, IItem used, IItem usedWith) =>
            TryOpenDialogue(character, used, usedWith, FletchingCategory.Wood, 3, definition => CreateFletchWoodCallback(character, definition));

        private Func<int, bool> CreateFletchWoodCallback(ICharacter character, FletchingDto definition)
        {
            return productIndex =>
            {
                var amount = 1;
                var resource = character.Inventory.Items.GetById(definition.ResourceId);
                if (resource == null)
                {
                    return true;
                }

                var slot = character.Inventory.Items.GetInstanceSlot(resource);
                if (slot == -1)
                {
                    return true;
                }

                var removed = character.Inventory.RemoveItem(resource);
                if (removed <= 0)
                {
                    return true;
                }

                amount = definition.ProductCounts[productIndex];
                var product = _itemBuilder.Create().WithId(definition.ProductIds[productIndex]).WithCount(amount).Build();
                character.Inventory.AddItem(product);
                if (amount > 1)
                {
                    character.SendChatMessage("You carefully cut the wood into " + amount + " " + product.Name.ToLower() + "s.");
                }
                else
                {
                    character.SendChatMessage("You carefully cut the wood into a " + product.Name.ToLower() + ".");
                }

                character.Statistics.AddExperience(StatisticsConstants.Fletching, definition.ProductExperiences[productIndex] * amount);
                return false;
            };
        }

        /// <summary>
        ///     Tries the fletch a bow (u) to a bow.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public bool TryFletchBow(ICharacter character, IItem used, IItem usedWith) =>
            TryOpenDialogue(character, used, usedWith, FletchingCategory.Bow, 2, definition => CreateFletchBowCallback(character, definition));

        private Func<int, bool> CreateFletchBowCallback(ICharacter character, FletchingDto definition)
        {
            return productIndex =>
            {
                if (!TryGetFletchingItems(character, definition, out var resource, out var resourceSlot, out var tool, out var toolSlot)) return true;

                var removed = character.Inventory.RemoveItem(resource, resourceSlot);
                if (removed <= 0)
                {
                    return true;
                }

                removed = character.Inventory.RemoveItem(tool, toolSlot);
                if (removed <= 0)
                {
                    return true;
                }

                var product = _itemBuilder.Create().WithId(definition.ProductIds[productIndex]).Build();
                character.Inventory.AddItem(resourceSlot, product);
                character.SendChatMessage("You add a " + tool.Name.ToLower() + " to the " + product.Name.ToLower() + ".");
                character.Statistics.AddExperience(StatisticsConstants.Fletching, definition.ProductExperiences[productIndex]);
                return false;
            };
        }

        /// <summary>
        ///     Tries the fletch ammunition.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public bool TryFletchAmmo(ICharacter character, IItem used, IItem usedWith) =>
            TryOpenDialogue(character, used, usedWith, FletchingCategory.Ammo, 1, definition => CreateFletchAmmoCallback(character, definition));

        private Func<int, bool> CreateFletchAmmoCallback(ICharacter character, FletchingDto definition)
        {
            return productIndex =>
            {
                if (!TryGetFletchingItems(character, definition, out var resource, out var resourceSlot, out var tool, out var toolSlot)) return true;

                var amount = definition.ProductCounts[productIndex];

                var itemAmount = character.Inventory.Items.GetCount(resource);
                if (itemAmount < amount)
                {
                    amount = itemAmount;
                }

                itemAmount = character.Inventory.Items.GetCount(tool);
                if (itemAmount < amount)
                {
                    amount = itemAmount;
                }

                var removed = character.Inventory.RemoveItem(_itemBuilder.Create().WithId(resource.Id).WithCount(amount).Build(), resourceSlot);
                if (removed < amount)
                {
                    amount = removed;
                }

                removed = character.Inventory.RemoveItem(_itemBuilder.Create().WithId(tool.Id).WithCount(amount).Build(), toolSlot);
                if (removed < amount)
                {
                    amount = removed;
                }

                if (amount < 0)
                {
                    return true;
                }

                var product = _itemBuilder.Create().WithId(definition.ProductIds[productIndex]).WithCount(amount).Build();
                character.Inventory.AddItem(product);
                character.SendChatMessage("You attach " + tool.Name.ToLower() + " to the " + product.Name.ToLower() + ".");
                character.Statistics.AddExperience(StatisticsConstants.Fletching, definition.ProductExperiences[productIndex] * amount);
                return false;
            };
        }

        /// <summary>
        ///     Tries the fletch tips.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public bool TryFletchTips(ICharacter character, IItem used, IItem usedWith) =>
            TryOpenDialogue(character, used, usedWith, FletchingCategory.Tips, 3, definition => CreateFletchTipsCallback(character, definition));

        private Func<int, bool> CreateFletchTipsCallback(ICharacter character, FletchingDto definition)
        {
            return productIndex =>
            {
                if (!TryGetFletchingItems(character, definition, out var resource, out var resourceSlot, out var tool, out _)) return true;

                var amount = definition.ProductCounts[productIndex];

                var removed = character.Inventory.RemoveItem(_itemBuilder.Create().WithId(resource.Id).Build(), resourceSlot);
                if (removed <= 0)
                {
                    return true;
                }

                var product = _itemBuilder.Create().WithId(definition.ProductIds[productIndex]).WithCount(amount).Build();
                character.Inventory.AddItem(product);
                character.SendChatMessage("You cut the " + resource.Name.ToLower() + " to " + amount + " x " + product.Name.ToLower() + ".");
                character.Statistics.AddExperience(StatisticsConstants.Fletching, definition.ProductExperiences[productIndex] * amount);
                return false;
            };
        }

        /// <summary>
        ///     Opens the make dialogue when the used items form a recipe of the category.
        /// </summary>
        private bool TryOpenDialogue(
            ICharacter character, IItem used, IItem usedWith, FletchingCategory category, int tickDelay, Func<FletchingDto, Func<int, bool>> createCallback)
        {
            var definition = _fletchingService.FindRecipe(category, used.Id, usedWith.Id);
            if (definition == null)
            {
                return false;
            }

            // check if the instances do exist, this is merely a check to prevent 'fake' items.
            if (character.Inventory.Items.GetInstanceSlot(used) == -1)
            {
                return false;
            }

            if (character.Inventory.Items.GetInstanceSlot(usedWith) == -1)
            {
                return false;
            }

            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = definition.ProductIds;
            dialogue.Options = InteractiveDialogueOptions.Make;
            dialogue.Info = "Choose how many you wish to make, <br>then click on the item to begin.";
            dialogue.PerformMakeProductCallback = (selectedItemID, currentCount) =>
            {
                if (currentCount > 0)
                {
                    var productIndex = Array.IndexOf(definition.ProductIds, selectedItemID);
                    TryStartFletching(character, definition, createCallback(definition), productIndex, tickDelay, currentCount);
                }

                return true;
            };


            var count = character.Inventory.Items.GetCountById(definition.ResourceId);
            dialogue.SetMaxCount(count, false);
            dialogue.SetCurrentCount(count, false);

            return InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }

        private static bool TryGetFletchingItems(
            ICharacter character,
            FletchingDto definition,
            out IItem resource,
            out int resourceSlot,
            out IItem tool,
            out int toolSlot)
        {
            resource = character.Inventory.Items.GetById(definition.ResourceId)!;
            if (resource == null)
            {
                tool = null!;
                resourceSlot = toolSlot = -1;
                return false;
            }

            resourceSlot = character.Inventory.Items.GetInstanceSlot(resource);
            if (resourceSlot == -1)
            {
                tool = null!;
                toolSlot = -1;
                return false;
            }

            tool = character.Inventory.Items.GetById(definition.ToolId)!;
            if (tool == null)
            {
                toolSlot = -1;
                return false;
            }

            toolSlot = character.Inventory.Items.GetInstanceSlot(tool);
            return toolSlot != -1;
        }

        private static void TryStartFletching(
            ICharacter character, FletchingDto definition, Func<int, bool> onFletchingPerformCallback, int productIndex, int tickDelay, int count)
        {
            var levelRequired = definition.RequiredLevels[productIndex];
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Fletching) < levelRequired)
            {
                character.SendChatMessage("You need a fletching level of " + levelRequired + " in order to fletch this.");
                return;
            }

            character.QueueTask(new FletchingTask(character, definition, productIndex, count, onFletchingPerformCallback, tickDelay));
        }
    }
}
