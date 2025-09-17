using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GameObject;
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
        ///     The TRE e_ ALEAD y_ CUT
        /// </summary>
        public const string TreeAlreadyCut = "Too late, someone else has already cut this tree down!";

        /// <summary>
        ///     The N o_ HATCHE t_ FOUND
        /// </summary>
        public const string NoHatchetFound = "You don't have any hatchets that you are able use.";

        /// <summary>
        ///     The N o_ INVENTOR y_ SPACE
        /// </summary>
        public const string NoInventorySpace = "You don't have enough space in your inventory to cut more logs.";

        /// <summary>
        ///     The LOG s_ RECIEVED
        /// </summary>
        public const string LogsReceived = "You get some logs.";

        /// <summary>
        ///     The SWIN g_ AXE
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

        /// <summary>
        ///     Attempts to find a hatchet in the specified character's inventory or equiped items.
        /// </summary>
        /// <param name="character">The character to find hatchet for.</param>
        /// <returns>Returns the hatchet type if found; Returns Hatchet.Undefined otherwise.</returns>
        private async Task<HatchetDto?> FindHatchet(ICharacter character)
        {
            var wcLevel = character.Statistics.GetSkillLevel(StatisticsConstants.Woodcutting);
            var service = _serviceProvider.GetRequiredService<IWoodcuttingService>();
            return (await service.FindAllHatchets())
                .Where(h => h.RequiredLevel <= wcLevel && (character.Equipment.GetById(h.ItemId) != null || character.Inventory.GetById(h.ItemId) != null))
                .OrderByDescending(h => h.RequiredLevel) // return the highest possible level
                .FirstOrDefault(); // return null if nothing found
        }

        /// <summary>
        ///     Starts the cutting.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="tree">The tree.</param>
        /// <returns></returns>
        public async Task<bool> StartCutting(ICharacter character, IGameObject tree)
        {
            var service = _serviceProvider.GetRequiredService<IWoodcuttingService>();
            var logs = await service.FindLogByTreeId(tree.Id);
            if (logs == null)
            {
                return false;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Woodcutting) < logs.RequiredLevel)
            {
                character.SendChatMessage("You must have a woodcutting level of " + logs.RequiredLevel + " or higher to cut this tree.");
                return true;
            }

            var treeDto = await service.FindTreeById(tree.Id);
            if (treeDto == null)
            {
                return false;
            }

            await StartCutting(character,
                tree,
                logs.BaseHarvestChance,
                logs.FallChance,
                logs.WoodcuttingExperience,
                treeDto.StumpId,
                logs.RespawnTime);
            return true;
        }

        /// <summary>
        ///     Attempts to cut some wood from a tree.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="tree">The tree.</param>
        /// <param name="treeBaseCutChance">The cut chance.</param>
        /// <param name="fallChance">The fall chance.</param>
        /// <param name="expReceived">The exp received.</param>
        /// <param name="stumpObjId">The stump object identifier.</param>
        /// <param name="respawnTime">The respawn time.</param>
        /// <param name="ivyTree">if set to <c>true</c> [ivy tree].</param>
        private async Task StartCutting(
            ICharacter character, IGameObject tree, double treeBaseCutChance, double fallChance, double expReceived, int stumpObjId, double respawnTime,
            bool ivyTree = false)
        {
            if (tree.IsDestroyed || tree.IsDisabled)
            {
                character.SendChatMessage(TreeAlreadyCut);
                return;
            }

            // check if the character has a hatchet on them (equipped or in inventory).
            var hatchetData = await FindHatchet(character);
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
            var cutChance = treeBaseCutChance + hatchetData.BaseHarvestChance;
            if (woodcuttingBasedChance > 0.0)
            {
                cutChance += woodcuttingBasedChance;
            }

            async ValueTask<bool> Callback()
            {
                if (character.Inventory.FreeSlots < 1)
                {
                    character.QueueAnimation(Animation.Create(-1));
                    character.SendChatMessage(NoInventorySpace);
                    return true; // stop cutting
                }

                var service = _serviceProvider.GetRequiredService<ILootService>();
                var table = await service.FindGameObjectLootTable(tree.Definition.LootTableId);
                if (table == null)
                {
                    return true;
                }

                character.Inventory.TryAddLoot(character, table, out var items);
                if (items.Any())
                {
                    character.SendChatMessage(LogsReceived);
                    character.Statistics.AddExperience(StatisticsConstants.Woodcutting, expReceived);
                }
                // Calculate the chance of the tree falling.
                var randomVal = RandomStatic.Generator.NextDouble();
                if (randomVal <= fallChance)
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
                        tree.Region.Remove(treeLeaves);
                    }

                    var goBuilder = _serviceProvider.GetRequiredService<IGameObjectBuilder>();
                    // spawn the stump if possible.
                    if (stumpObjId > 0)
                    {
                        var stumpObj = goBuilder.Create()
                            .WithId(stumpObjId)
                            .WithLocation(tree.Location)
                            .WithRotation(tree.Rotation)
                            .WithShape(tree.ShapeType)
                            .Build();
                        tree.Region.Add(stumpObj);
                    }
                    else // delete the tree object.
                    {
                        tree.Region.Remove(tree);
                    }

                    var characterCount = await _characterStore.CountAsync();
                    var respawnTick = (int)(respawnTime * (1.0 + characterCount * -0.00025) * 100.0);
                    // register a task that will respawn the tree once it has reached the respawn rate.
                    _rsTaskService.Schedule(new RsTask(() =>
                        {
                            tree.Region.Add(tree);
                            if (treeLeaves != null)
                            {
                                tree.Region.Add(treeLeaves);
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