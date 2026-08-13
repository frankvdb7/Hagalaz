using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Resources;

namespace Hagalaz.Game.Scripts.Skills.Woodcutting
{
    /// <summary>
    ///     Functionality for the woodcutting skill.
    /// </summary>
    public class WoodcuttingSkillService : IWoodcuttingSkillService
    {
        /// <summary>
        ///     The tree already cut message.
        /// </summary>
        public const string TreeAlreadyCut = "Too late, someone else has already cut this tree down!";

        /// <summary>
        ///     The no hatchet found message.
        /// </summary>
        public const string NoHatchetFound = "You don't have any hatchets that you are able use.";

        /// <summary>
        ///     The no inventory space message.
        /// </summary>
        public const string NoInventorySpace = "You don't have enough space in your inventory to cut more logs.";

        /// <summary>
        ///     The logs received message.
        /// </summary>
        public const string LogsReceived = "You get some logs.";

        /// <summary>
        ///     The swing axe message.
        /// </summary>
        public const string SwingAxe = "You swing your hatchet at the tree.";

        private readonly IServiceProvider _serviceProvider;
        private readonly ICharacterStore _characterStore;
        private readonly IRsTaskService _rsTaskService;

        public WoodcuttingSkillService(IServiceProvider serviceProvider, ICharacterStore characterStore, IRsTaskService rsTaskService)
        {
            _serviceProvider = serviceProvider.CreateScope().ServiceProvider;
            _characterStore = characterStore;
            _rsTaskService = rsTaskService;
        }

        private static HatchetDto? FindHatchet(ICharacter character, IReadOnlyList<HatchetDto> hatchets)
        {
            var wcLevel = character.Statistics.GetSkillLevel(StatisticsConstants.Woodcutting);
            return hatchets
                .Where(h => h.RequiredLevel <= wcLevel &&
                            (character.Equipment.GetById(h.ItemId) != null || character.Inventory.GetById(h.ItemId) != null))
                .OrderByDescending(h => h.RequiredLevel)
                .FirstOrDefault();
        }

        /// <summary>
        ///     Loads immutable skill definitions asynchronously and returns the game-loop continuation.
        /// </summary>
        public async Task<Action?> PrepareCutting(ICharacter character, IGameObject tree)
        {
            var service = _serviceProvider.GetRequiredService<IWoodcuttingService>();
            var logs = await service.FindLogByTreeId(tree.Id);
            if (logs == null)
            {
                return null;
            }

            var treeDto = await service.FindTreeById(tree.Id);
            if (treeDto == null)
            {
                return null;
            }

            var hatchets = await service.FindAllHatchets();
            var lootService = _serviceProvider.GetRequiredService<ILootService>();
            var lootTable = await lootService.FindGameObjectLootTable(tree.Definition.LootTableId);
            var characterCount = await _characterStore.CountAsync();
            return () => BeginCutting(character, tree, logs, treeDto, hatchets, lootTable, characterCount);
        }

        private void BeginCutting(
            ICharacter character,
            IGameObject tree,
            LogDto logs,
            TreeDto treeDto,
            IReadOnlyList<HatchetDto> hatchets,
            ILootTable? lootTable,
            int characterCount,
            bool ivyTree = false)
        {
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Woodcutting) < logs.RequiredLevel)
            {
                character.SendChatMessage("You must have a woodcutting level of " + logs.RequiredLevel + " or higher to cut this tree.");
                return;
            }

            if (tree.IsDestroyed || tree.IsDisabled)
            {
                character.SendChatMessage(TreeAlreadyCut);
                return;
            }

            // check if the character has a hatchet on them (equipped or in inventory).
            var hatchetData = FindHatchet(character, hatchets);
            if (hatchetData == null)
            {
                character.SendChatMessage(NoHatchetFound);
                return;
            }

            // check if there is enough space in the character's inventory.
            if (character.Inventory.FreeSlots < 1)
            {
                character.SendChatMessage(GameStrings.InventoryFull);
                return;
            }

            var woodcuttingBasedChance = Math.Log10(Math.Log10(character.Statistics.GetSkillLevel(StatisticsConstants.Woodcutting))) * 0.075;
            var cutChance = logs.BaseHarvestChance + hatchetData.BaseHarvestChance;
            if (woodcuttingBasedChance > 0.0)
            {
                cutChance += woodcuttingBasedChance;
            }

            if (lootTable == null)
            {
                return;
            }

            bool Callback()
            {
                if (character.Inventory.FreeSlots < 1)
                {
                    character.QueueAnimation(Animation.Create(-1));
                    character.SendChatMessage(NoInventorySpace);
                    return true; // stop cutting
                }

                character.Inventory.TryAddLoot(character, lootTable, out var items);
                if (items.Any())
                {
                    character.SendChatMessage(LogsReceived);
                    character.Statistics.AddExperience(StatisticsConstants.Woodcutting, logs.WoodcuttingExperience);
                }

                // Calculate the chance of the tree falling.
                var randomVal = RandomStatic.Generator.NextDouble();
                if (randomVal <= logs.FallChance)
                {
                    character.QueueAnimation(Animation.Create(-1));

                    var gameObjectService = _serviceProvider.GetRequiredService<IGameObjectService>();

                    var treeLeaves = gameObjectService
                                         .FindByLocation(tree.Location.Translate(0, 0, 1))
                                         .FindByStandardObject()
                                         .FirstOrDefault()
                                     ?? gameObjectService
                                         .FindByLocation(tree.Location.Translate(-1, -1, 1))
                                         .FindByStandardObject()
                                         .FirstOrDefault();

                    // new trees have leaves, so remove the leaves if possible.
                    if (treeLeaves != null)
                    {
                        character.ServiceProvider.GetRequiredService<IMapRegionService>()
                            .GetOrCreateMapRegion(tree.Location.RegionId, tree.Location.Dimension, false)
                            .Remove(treeLeaves);
                    }

                    var goBuilder = _serviceProvider.GetRequiredService<IGameObjectBuilder>();
                    // spawn the stump if possible.
                    if (treeDto.StumpId > 0)
                    {
                        var stumpObj = goBuilder.Create()
                            .WithId(treeDto.StumpId)
                            .WithLocation(tree.Location)
                            .WithRotation(tree.Rotation)
                            .WithShape(tree.ShapeType)
                            .Build();
                        character.ServiceProvider.GetRequiredService<IMapRegionService>()
                            .GetOrCreateMapRegion(tree.Location.RegionId, tree.Location.Dimension, false)
                            .Add(stumpObj);
                    }
                    else // delete the tree object.
                    {
                        character.ServiceProvider.GetRequiredService<IMapRegionService>()
                            .GetOrCreateMapRegion(tree.Location.RegionId, tree.Location.Dimension, false)
                            .Remove(tree);
                    }

                    var respawnTick = (int)(logs.RespawnTime * (1.0 + characterCount * -0.00025) * 100.0);
                    // register a task that will respawn the tree once it has reached the respawn rate.
                    _rsTaskService.Schedule(new RsTask(() =>
                        {
                            character.ServiceProvider.GetRequiredService<IMapRegionService>()
                                .GetOrCreateMapRegion(tree.Location.RegionId, tree.Location.Dimension, false)
                                .Add(tree);
                            if (treeLeaves != null)
                            {
                                character.ServiceProvider.GetRequiredService<IMapRegionService>()
                                    .GetOrCreateMapRegion(tree.Location.RegionId, tree.Location.Dimension, false)
                                    .Add(treeLeaves);
                            }
                        },
                        respawnTick));
                    return true;
                }

                return false; // keep cutting
            }

            // queue the woodcutting task.
            character.QueueTask(new WoodcuttingTask(character, Callback, cutChance, hatchetData, tree, ivyTree));
            character.QueueAnimation(Animation.Create(ivyTree ? hatchetData.CanoeAnimationId : hatchetData.ChopAnimationId));
            character.SendChatMessage(SwingAxe);
        }
    }
}
