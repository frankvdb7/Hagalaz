using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
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

        public async Task StartCuttingAsync(
            ICharacter character,
            EntityHandle<IGameObject> treeHandle,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var entityService = character.ServiceProvider.GetRequiredService<IEntityService>();
            if (!entityService.TryResolve(treeHandle, out var initialTree)
                || initialTree is null
                || initialTree.IsDisabled)
            {
                return;
            }

            var treeId = initialTree.Id;
            var lootTableId = initialTree.Definition.LootTableId;
            var interrupted = false;
            var interruptEvent = character.RegisterEventHandler<CreatureInterruptedEvent>(_ =>
            {
                interrupted = true;
                return false;
            });

            try
            {
                var service = _serviceProvider.GetRequiredService<IWoodcuttingService>();
                var logs = await service.FindLogByTreeId(treeId);
                if (logs is null)
                {
                    return;
                }

                var treeDto = await service.FindTreeById(treeId);
                if (treeDto is null)
                {
                    return;
                }

                var hatchets = await service.FindAllHatchets();
                var lootService = _serviceProvider.GetRequiredService<ILootService>();
                var lootTable = await lootService.FindGameObjectLootTable(lootTableId);
                var characterCount = await _characterStore.CountAsync();
                cancellationToken.ThrowIfCancellationRequested();

                if (!interrupted
                    && entityService.TryResolve(treeHandle, out var currentTree)
                    && currentTree is not null
                    && !currentTree.IsDisabled)
                {
                    StartCutting(character, treeHandle, logs, treeDto, hatchets, lootTable, characterCount);
                }
            }
            finally
            {
                character.UnregisterEventHandler<CreatureInterruptedEvent>(interruptEvent);
            }
        }

        private void StartCutting(
            ICharacter character,
            EntityHandle<IGameObject> treeHandle,
            LogDto logs,
            TreeDto treeDto,
            IReadOnlyList<HatchetDto> hatchets,
            ILootTable? lootTable,
            int characterCount,
            bool ivyTree = false)
        {
            var entityService = character.ServiceProvider.GetRequiredService<IEntityService>();
            if (!entityService.TryResolve(treeHandle, out var tree) || tree is null || tree.IsDisabled)
            {
                if (tree?.IsDisabled == true)
                {
                    character.SendChatMessage(TreeAlreadyCut);
                }

                return;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Woodcutting) < logs.RequiredLevel)
            {
                character.SendChatMessage("You must have a woodcutting level of " + logs.RequiredLevel + " or higher to cut this tree.");
                return;
            }

            if (tree.IsDisabled)
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

            bool Callback(IGameObject target)
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
                                         .FindByLocation(target.Location.Translate(0, 0, 1))
                                         .FindByStandardObject()
                                         .FirstOrDefault()
                                     ?? gameObjectService
                                         .FindByLocation(target.Location.Translate(-1, -1, 1))
                                         .FindByStandardObject()
                                         .FirstOrDefault();
                    var treeLeavesHandle = treeLeaves?.Handle;

                    // new trees have leaves, so remove the leaves if possible.
                    if (treeLeaves != null)
                    {
                        character.ServiceProvider.GetRequiredService<IMapRegionService>().RemoveGameObject(treeLeaves);
                    }

                    var goBuilder = _serviceProvider.GetRequiredService<IGameObjectBuilder>();
                    // spawn the stump if possible.
                    if (treeDto.StumpId > 0)
                    {
                        var stumpObj = goBuilder.Create()
                            .WithId(treeDto.StumpId)
                            .WithLocation(target.Location)
                            .WithRotation(target.Rotation)
                            .WithShape(target.ShapeType)
                            .Build();
                        character.ServiceProvider.GetRequiredService<IMapRegionService>().AddGameObject(stumpObj);
                    }
                    else // delete the tree object.
                    {
                        character.ServiceProvider.GetRequiredService<IMapRegionService>().RemoveGameObject(target);
                    }

                    var respawnTick = (int)(logs.RespawnTime * (1.0 + characterCount * -0.00025) * 100.0);
                    var targetHandle = target.Handle;
                    // register a task that will respawn the tree once it has reached the respawn rate.
                    _rsTaskService.Schedule(new RsTask(() =>
                        {
                            var mapRegionService = character.ServiceProvider.GetRequiredService<IMapRegionService>();
                            if (entityService.TryResolve(targetHandle, out var respawnTree)
                                && respawnTree is not null
                                && respawnTree.IsDisabled)
                            {
                                mapRegionService.AddGameObject(respawnTree);
                            }

                            if (treeLeavesHandle is { } leavesHandle
                                && entityService.TryResolve(leavesHandle, out var respawnLeaves)
                                && respawnLeaves is not null
                                && respawnLeaves.IsDisabled)
                            {
                                mapRegionService.AddGameObject(respawnLeaves);
                            }
                        },
                        respawnTick));
                    return true;
                }

                return false; // keep cutting
            }

            // queue the woodcutting task.
            if (!entityService.TryResolve(treeHandle, out var currentTreeBeforeQueue)
                || currentTreeBeforeQueue is null
                || currentTreeBeforeQueue.IsDisabled)
            {
                return;
            }

            character.QueueTask(new WoodcuttingTask(character, Callback, cutChance, hatchetData, treeHandle, ivyTree));
            character.QueueAnimation(Animation.Create(ivyTree ? hatchetData.CanoeAnimationId : hatchetData.ChopAnimationId));
            character.SendChatMessage(SwingAxe);
        }
    }
}
