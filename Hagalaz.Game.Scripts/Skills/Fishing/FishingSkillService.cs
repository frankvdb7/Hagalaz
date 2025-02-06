using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Resources;
using static Hagalaz.Game.Scripts.Resources.Messages;

namespace Hagalaz.Game.Scripts.Skills.Fishing
{
    /// <summary>
    ///     Functionality and information for the fishing skill.
    /// </summary>
    public class FishingSkillService : IFishingSkillService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICharacterStore _characterStore;
        private readonly IRsTaskService _rsTaskService;
        private readonly IItemBuilder _itemBuilder;
        private readonly ILootGenerator _lootGenerator;

        public FishingSkillService(IServiceProvider serviceProvider, ICharacterStore characterStore, IRsTaskService rsTaskService, IItemBuilder itemBuilder)
        {
            _serviceProvider = serviceProvider.CreateScope().ServiceProvider;
            _characterStore = characterStore;
            _rsTaskService = rsTaskService;
            _itemBuilder = itemBuilder;
            _lootGenerator = serviceProvider.GetRequiredService<ILootGenerator>();
        }

        /// <summary>
        ///     Fishes the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="fishingSpot">The fishing spot.</param>
        /// <param name="table">The definition.</param>
        /// <returns></returns>
        public async Task<bool> TryFish(ICharacter character, INpc fishingSpot, IFishingSpotTable? table)
        {
            await Task.CompletedTask;
            if (table == null || fishingSpot.IsDestroyed || !fishingSpot.Appearance.Visible)
            {
                return false;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Fishing) < table.MinimumLevel)
            {
                character.SendChatMessage(string.Format(You_must_have_a_fishing_level_of_X_or_higher_to_fish_here, table.MinimumLevel));
                return true;
            }

            if (character.Inventory.FreeSlots < 1)
            {
                character.SendChatMessage(GameStrings.InventoryFull);
                return true;
            }

            var itemService = _serviceProvider.GetRequiredService<IItemService>();
            // check if character has required tool...
            if (!character.Inventory.Contains(table.RequiredTool.ItemId))
            {
                character.SendChatMessage(string.Format(You_need_a_X_in_order_to_fish_at_this_spot,
                    itemService.FindItemDefinitionById(table.RequiredTool.ItemId).Name.ToLower()));
                return true;
            }

            if (table.BaitId > 0 && !character.Inventory.Contains(table.BaitId))
            {
                character.SendChatMessage(string.Format(You_need_X_in_order_to_fish_at_this_spot,
                    itemService.FindItemDefinitionById(table.BaitId).Name.ToLower()));
                return true;
            }

            var toolName = itemService.FindItemDefinitionById(table.RequiredTool.ItemId).Name.ToLower();

            var fishingBasedChance = Math.Log10(Math.Log10(character.Statistics.GetSkillLevel(StatisticsConstants.Fishing))) * 0.075;
            var fishChance = table.BaseCatchChance; // base catch chance
            if (fishingBasedChance > 0.0)
            {
                fishChance += fishingBasedChance;
            }

            async ValueTask<bool> Callback()
            {
                foreach (var result in _lootGenerator.GenerateLoot<IFishingLoot>(new CharacterLootParams(table, character)))
                {
                    var fish = _itemBuilder.Create().WithId(result.Item.Id).Build();
                    if (!character.Inventory.Add(fish))
                    {
                        continue;
                    }

                    character.Statistics.AddExperience(StatisticsConstants.Fishing, result.Item.FishingExperience);
                    var itemName = fish.ItemDefinition.Name.ToLower().Replace("raw ", "");
                    if (toolName.Contains("small fishing net"))
                    {
                        character.SendChatMessage(string.Format(You_catch_some_X, itemName));
                    }
                    else if (fish.Id == 383) // shark
                    {
                        character.SendChatMessage(string.Format(You_catch_a_shark, itemName));
                    }
                    else
                    {
                        character.SendChatMessage(string.Format(You_catch_a_X, itemName));
                    }
                }

                // Calculate the chance of the spot exhaust
                var randomVal = RandomStatic.Generator.NextDouble();
                if (randomVal <= table.ExhaustChance)
                {
                    character.QueueAnimation(Animation.Create(-1));

                    var characterCount = await _characterStore.FindAllAsync().CountAsync();
                    var respawnTick = (int)(table.RespawnTime * (1.0 + characterCount * -0.00025) * 100.0);

                    fishingSpot.Appearance.Visible = false;

                    _rsTaskService.Schedule(new RsTask(() => { fishingSpot.Appearance.Visible = true; }, respawnTick));
                    return true;
                }

                // No more space left to keep fishing.
                if (character.Inventory.FreeSlots < 1)
                {
                    character.QueueAnimation(Animation.Create(-1));
                    character.SendChatMessage(You_cant_carry_any_more_fish);
                    return true; // stop fishing
                }

                if (table.BaitId <= 0 || (character.Inventory.Remove(_itemBuilder.Create().WithId(table.BaitId).Build()) > 0 &&
                                          character.Inventory.Contains(table.BaitId)))
                {
                    return false; // keep fishing
                }

                character.QueueAnimation(Animation.Create(-1));
                character.SendChatMessage(string.Format(You_need_X_in_order_to_fish_at_this_spot,
                    itemService.FindItemDefinitionById(table.BaitId).Name.ToLower()));
                return true; // stop fishing
            }

            // queue the fishing task.
            character.QueueTask(new FishingTask(character, Callback, fishChance, fishingSpot, table.RequiredTool.FishAnimationId));
            if (table.RequiredTool.CastAnimationId > 0)
            {
                character.QueueAnimation(Animation.Create(table.RequiredTool.CastAnimationId));
            }

            if (toolName.Contains("net"))
            {
                character.SendChatMessage(You_cast_out_your_net);
            }
            else if (toolName.Contains("rod"))
            {
                character.SendChatMessage(You_cast_out_your_line);
                character.SendChatMessage(You_attempt_to_catch_a_fish);
            }
            else if (toolName.Contains("pot"))
            {
                character.SendChatMessage(You_attempt_to_catch_a_lobster);
            }
            else if (toolName.Contains("harpoon"))
            {
                character.SendChatMessage(You_start_harpooning_fish);
            }

            return true;
        }
    }
}