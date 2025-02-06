using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Tasks;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    public class HerbloreSkillService : IHerbloreSkillService
    {
        private readonly IItemBuilder _itemBuilder;
        private readonly IHerbloreService _herbloreService;
        private static readonly int[] _productIds = [15333];

        public HerbloreSkillService(IServiceProvider serviceProvider, IItemBuilder itemBuilder)
        {
            _itemBuilder = itemBuilder;
            _herbloreService = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IHerbloreService>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="item"></param>
        public async Task TryCleanHerb(ICharacter character, IItem item)
        {
            var service = character.ServiceProvider.GetRequiredService<IHerbloreService>();
            var herb = await service.FindGrimyHerbById(item.Id);
            if (herb == null)
            {
                return;
            }

            var defaultScript = character.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
            character.Widgets.OpenChatboxOverlay((int)DialogueInterfaces.InteractiveChatBox, 0, defaultScript, false);
            var parent = character.Widgets.GetOpenWidget((int)DialogueInterfaces.InteractiveChatBox);
            if (parent == null)
            {
                return;
            }

            var herbloreDialogue = character.ServiceProvider.GetRequiredService<HerbloreDialogue>();
            herbloreDialogue.Definition = herb;
            herbloreDialogue.TickDelay = 1;
            character.Widgets.OpenWidget((int)DialogueInterfaces.InteractiveSelectAmountBox, parent, 4, 0, herbloreDialogue, false);
        }

        /// <summary>
        ///     Gets the potion id by the primary item.
        /// </summary>
        /// <returns></returns>
        public PotionDto? GetPotionByPrimaryItemId(int id)
        {
            var potions = _herbloreService.FindAllPotions().Result;
            return potions.FirstOrDefault(p => p.PrimaryItemIds.Contains(id));
        }

        /// <summary>
        ///     Gets the potion id by the secondary item.
        /// </summary>
        /// <returns></returns>
        public PotionDto? GetPotionBySecondaryItemId(int unfinishedPotionId, int secondaryItemId)
        {
            var potions = _herbloreService.FindAllPotions().Result;
            return potions.FirstOrDefault(p => p.UnfinishedPotionId == unfinishedPotionId && p.SecondaryItemIds.Contains(secondaryItemId));
        }

        /// <summary>
        ///     Gets the unfinished potion id.
        /// </summary>
        /// <returns></returns>
        public PotionDto? GetPotionByUnfinishedPotionId(int unfinishedPotionId)
        {
            var potions = _herbloreService.FindAllPotions().Result;
            return potions.FirstOrDefault(p => p.UnfinishedPotionId == unfinishedPotionId);
        }

        /// <summary>
        ///     Makes the overload.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="herb">The herb.</param>
        /// <returns></returns>
        public bool MakeOverload(ICharacter character, IItem herb)
        {
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Herblore) < 96)
            {
                character.SendChatMessage("You need herblore level of 96 to make this potion.");
                return true;
            }

            var resourceCount = character.Inventory.GetCountById(herb.Id);
            foreach (var t in HerbloreConstants.OverloadPotions)
            {
                var characterCount = character.Inventory.GetCountById(t);
                if (characterCount > 0 && characterCount < resourceCount)
                {
                    resourceCount = characterCount;
                }
            }

            var service = character.ServiceProvider.GetRequiredService<IItemService>();
            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = _productIds;
            dialogue.PerformMakeProductCallback = (itemID, count) =>
            {
                // the character has selected an amount, we then queue an task that will create all products
                character.QueueTask(new MakeProductTask(count,
                    3,
                    () =>
                    {
                        var slot = character.Inventory.GetInstanceSlot(herb);
                        if (slot == -1)
                        {
                            return true;
                        }

                        foreach (var t in HerbloreConstants.OverloadPotions)
                        {
                            if (character.Inventory.Contains(t))
                            {
                                continue;
                            }

                            character.SendChatMessage("You need a(n) " + service.FindItemDefinitionById(t).Name + " in order to make this potion.");
                            return true;
                        }

                        foreach (var t in HerbloreConstants.OverloadPotions)
                        {
                            character.Inventory.Remove(_itemBuilder.Create().WithId(t).Build());
                        }

                        character.Inventory.Replace(slot, _itemBuilder.Create().WithId(15333).Build());
                        character.QueueAnimation(Animation.Create(HerbloreConstants.MakePotionAnimation));
                        character.Statistics.AddExperience(StatisticsConstants.Herblore, 1000);
                        return false;
                    }));
                return true;
            };
            dialogue.SetMaxCount(resourceCount, false);
            dialogue.SetCurrentCount(resourceCount, false);
            // open a dialogue for the character to select an amount and such
            return InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }

        /// <summary>
        ///     Makes the finished potion.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="vial">The vial.</param>
        /// <param name="ingredient">The vial with.</param>
        /// <param name="potion">The potion.</param>
        /// <param name="makeUnfinished">if set to <c>true</c> [make unfinished].</param>
        /// <returns></returns>
        public bool MakePotion(ICharacter character, IItem vial, IItem ingredient, PotionDto potion, bool makeUnfinished)
        {
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Herblore) < potion.RequiredLevel)
            {
                character.SendChatMessage("You need herblore level of " + potion.RequiredLevel + " to make this potion.");
                return true;
            }

            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = [makeUnfinished ? potion.UnfinishedPotionId : potion.PotionId];
            dialogue.PerformMakeProductCallback = (itemID, count) =>
            {
                // the character has selected an amount, we then queue an task that will create all products
                character.QueueTask(new MakeProductTask(count,
                    2,
                    () =>
                    {
                        var inventoryResource = character.Inventory.GetById(ingredient.Id);
                        if (inventoryResource == null)
                        {
                            return true;
                        }

                        var inventoryVail = character.Inventory.GetById(vial.Id);
                        if (inventoryVail == null)
                        {
                            return true;
                        }

                        var itemSlot = character.Inventory.GetSlotByItem(inventoryResource);
                        if (itemSlot == -1)
                        {
                            return true;
                        }

                        var vialSlot = character.Inventory.GetSlotByItem(inventoryVail);
                        if (vialSlot == -1)
                        {
                            return true;
                        }

                        character.Inventory.Remove(inventoryResource.Id == HerbloreConstants.Grenwallspikes
                                ? _itemBuilder.Create().WithId(HerbloreConstants.Grenwallspikes).WithCount(5).Build()
                                : inventoryResource,
                            itemSlot);
                        character.Inventory.Replace(vialSlot,
                            _itemBuilder.Create().WithId(makeUnfinished ? potion.UnfinishedPotionId : potion.PotionId).Build());
                        character.QueueAnimation(Animation.Create(HerbloreConstants.MakePotionAnimation));
                        if (!makeUnfinished)
                        {
                            character.Statistics.AddExperience(StatisticsConstants.Herblore, potion.Experience);
                        }

                        return false;
                    }));
                return true;
            };
            var resCount = character.Inventory.GetCountById(ingredient.Id);
            dialogue.SetMaxCount(resCount, false);
            dialogue.SetCurrentCount(resCount, false);

            // open a dialogue for the character to select an amount and such
            return InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }

        /// <summary>
        ///     Cleans the herb.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="cleanCount">The clean count.</param>
        /// <param name="tickDelay">The tick delay.</param>
        /// <returns></returns>
        public bool QueueCleanHerbTask(ICharacter character, HerbDto definition, int cleanCount, int tickDelay)
        {
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Herblore) < definition.CleanLevel)
            {
                character.SendChatMessage("You need herblore level of " + definition.CleanLevel + " to clean this herb.");
                return false;
            }

            character.QueueTask(new CleanHerbTask(character, definition, cleanCount, tickDelay));
            return true;
        }
    }
}