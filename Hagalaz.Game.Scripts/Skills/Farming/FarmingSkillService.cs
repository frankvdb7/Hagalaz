using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Farming
{
    /// <summary>
    /// </summary>
    public class FarmingSkillService : IFarmingSkillService
    {
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        ///     The empty bucket identifier.
        /// </summary>
        private const int _emptyBucketID = 1925;

        /// <summary>
        ///     The spade identifier.
        /// </summary>
        private const int _spadeID = 952;

        public FarmingSkillService(IItemBuilder itemBuilder)
        {
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Handles the patch click perform.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="obj">The object.</param>
        /// <param name="clickType">Type of the click.</param>
        public void HandlePatchClickPerform(ICharacter character, IGameObject obj, GameObjectClickType clickType)
        {
            var patch = character.Farming.GetFarmingPatch(obj.Id);
            if (patch == null)
            {
                return;
            }

            switch (clickType)
            {
                case GameObjectClickType.Option1Click when !patch.HasCondition(PatchCondition.Cleared): HandlePatchRaking(character, patch, obj.Id); break;
                case GameObjectClickType.Option1Click: HandlePatchClick(character, patch); break;
                case GameObjectClickType.Option2Click when !patch.HasCondition(PatchCondition.Cleared):
                    character.SendChatMessage("This patch needs weeding.");
                    break;
                case GameObjectClickType.Option2Click when patch.HasCondition(PatchCondition.Dead):
                    character.SendChatMessage("The plants in this patch have died.");
                    break;
                case GameObjectClickType.Option2Click when patch.HasCondition(PatchCondition.Diseased):
                    character.SendChatMessage("The plants in this patch are diseased.");
                    break;
                case GameObjectClickType.Option2Click when patch.HasCondition(PatchCondition.Fertilized) || patch.HasCondition(PatchCondition.SuperFertilized):
                    character.SendChatMessage("This patch is saturated by compost.");
                    break;
                case GameObjectClickType.Option2Click when patch.HasCondition(PatchCondition.Planted):
                    character.SendChatMessage("The plants in this patch are healthy.");
                    break;
                case GameObjectClickType.Option2Click: character.SendChatMessage("This patch is ready for planting."); break;
                case GameObjectClickType.Option3Click:
                    {
                        if (patch.HasCondition(PatchCondition.Diseased) || patch.PatchDefinition.Type == PatchType.Tree ||
                            patch.PatchDefinition.Type == PatchType.FruitTree || patch.PatchDefinition.Type == PatchType.Bush)
                        {
                            HandlePatchClear(character, patch);
                        }

                        break;
                    }
                case GameObjectClickType.Option4Click:
                    // show guide
                    break;
            }
        }

        /// <summary>
        ///     Handles the patch raking.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="patch">The patch.</param>
        /// <param name="objID">The object identifier.</param>
        private void HandlePatchRaking(ICharacter character, IFarmingPatch? patch, int objID)
        {
            if (!character.Inventory.Contains(5341))
            {
                character.SendChatMessage("You need a rake to get rid of the weeds.");
                return;
            }

            if (patch == null)
            {
                patch = character.Farming.GetOrCreateFarmingPatch(objID);
            }

            character.QueueAnimation(Animation.Create(2273));
            RsTickTask task = null!;
            task = new RsTickTask(() =>
            {
                if (task.TickCount % 2 != 0)
                {
                    return;
                }

                if (RandomStatic.Generator.Next(0, 3) == 0)
                {
                    character.Inventory.TryAddItems(character, [(6055, 1)], out _);
                    character.Statistics.AddExperience(StatisticsConstants.Farming, 4);
                    patch.IncrementCycle();
                    if (patch.CurrentCycle == 3)
                    {
                        patch.AddCondition(PatchCondition.Cleared);
                        patch.Refresh();
                        task.Cancel();
                        return;
                    }

                    patch.Refresh();
                }

                character.QueueAnimation(Animation.Create(2273));
            }); // can interrupt
            EventHappened? @event = null;
            @event = character.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                task.Cancel();
                character.UnregisterEventHandler<CreatureInterruptedEvent>(@event);
                return false;
            });
            character.QueueTask(task);
        }

        /// <summary>
        ///     Handles the patch left click.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="patch">The patch.</param>
        private void HandlePatchClick(ICharacter character, IFarmingPatch patch)
        {
            switch (patch.PatchDefinition.Type)
            {
                case PatchType.Bush:
                case PatchType.FruitTree:
                case PatchType.Tree:
                    if (patch.HasCondition(PatchCondition.Mature) && !patch.HasCondition(PatchCondition.Checked))
                    {
                        HandlePatchCheckHealth(character, patch);
                    }
                    /*else if (patch.HasCondition(PatchCondition.Mature) && patch.HasCondition(PatchCondition.Checked))
                        Woodcutting.Woodcutting.StartCuttingAsync(character, ) TODO: Insert actual woodcutting. */
                    else if (patch.HasCondition(PatchCondition.Dead))
                    {
                        HandlePatchClear(character, patch);
                    }
                    else if (patch.HasCondition(PatchCondition.Diseased))
                    {
                        HandlePatchCuring(character, null, patch);
                    }

                    break;
                default:
                    if (patch.HasCondition(PatchCondition.Mature))
                    {
                        HandlePatchHarvest(character, patch);
                    }
                    else if (patch.HasCondition(PatchCondition.Dead))
                    {
                        HandlePatchClear(character, patch);
                    }
                    else if (patch.HasCondition(PatchCondition.Diseased))
                    {
                        HandlePatchCuring(character, null, patch);
                    }

                    break;
            }
        }

        /// <summary>
        ///     Handles the check patch health.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="patch">The patch.</param>
        private void HandlePatchCheckHealth(ICharacter character, IFarmingPatch patch)
        {
            character.SendChatMessage("You examine the " + patch.PatchDefinition.Type.ToString().ToLower() +
                                      " for signs of disease, and quickly realise that it is at full health.");
            character.Statistics.AddExperience(StatisticsConstants.Farming, patch.Seed?.HarvestExperience ?? 0);
            character.QueueAnimation(Animation.Create(832));
            patch.AddCondition(PatchCondition.Checked);
            patch.Refresh();
        }

        /// <summary>
        ///     Handles the patch harvesting.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="patch">The patch.</param>
        private void HandlePatchHarvest(ICharacter character, IFarmingPatch patch)
        {
            if (!character.Inventory.Contains(_spadeID))
            {
                character.SendChatMessage("You need a spade to harvest your crops.");
                return;
            }

            var harvestAnimation = GetHarvestAnimation(patch.PatchDefinition.Type);
            character.QueueAnimation(Animation.Create(harvestAnimation));
            character.SendChatMessage("You begin to harvest the " + patch.PatchDefinition.Type.ToString().ToLower() + " patch.");
            RsTickTask task = null!;
            task = new RsTickTask(() =>
            {
                if (task.TickCount % 2 != 0)
                {
                    return;
                }

                character.Inventory.TryAddItems(character, [(patch.Seed!.ProductID, 1)], out _);
                character.Statistics.AddExperience(StatisticsConstants.Farming, patch.Seed.HarvestExperience);
                patch.Harvest();
                if (!patch.HasCondition(PatchCondition.Planted))
                {
                    character.SendChatMessage("The " + patch.PatchDefinition.Type.ToString().ToLower() + " patch is now empty.");
                    task.Cancel();
                    return;
                }

                character.QueueAnimation(Animation.Create(harvestAnimation));
            });
            EventHappened @event = null;
            @event = character.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                task.Cancel();
                character.UnregisterEventHandler<CreatureInterruptedEvent>(@event);
                return false;
            });
            character.QueueTask(task);
        }

        /// <summary>
        ///     Handles the patch clear.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="patch">The patch.</param>
        private void HandlePatchClear(ICharacter character, IFarmingPatch patch)
        {
            if (!character.Inventory.Contains(_spadeID))
            {
                character.SendChatMessage("You need a spade to clear this " + patch.PatchDefinition.Type.ToString().ToLower() + " patch.");
                return;
            }

            character.SendChatMessage("You start digging up the " + patch.PatchDefinition.Type.ToString().ToLower() + " patch.");
            RsTickTask task = null;
            task = new RsTickTask(() =>
            {
                if (task.TickCount % 2 != 0)
                {
                    return;
                }

                if (RandomStatic.Generator.Next(0, 4) == 2)
                {
                    character.SendChatMessage("The " + patch.PatchDefinition.Type.ToString().ToLower() + " patch is now empty.");
                    patch.Clear();
                    task.Cancel();
                    return;
                }

                character.QueueAnimation(Animation.Create(830)); // shovel anim
            });
            EventHappened @event = null;
            @event = character.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                task.Cancel();
                character.UnregisterEventHandler<CreatureInterruptedEvent>(@event);
                return false;
            });
            character.QueueTask(task);
        }

        /// <summary>
        ///     Handles the patch item.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public async Task<bool> HandlePatchItem(ICharacter character, IItem item, IGameObject obj)
        {
            var patch = character.Farming.GetOrCreateFarmingPatch(obj.Id);
            if (patch.HasCondition(PatchCondition.Mature))
            {
                return false;
            }

            switch (item.Id)
            {
                case 5333: // watering cans
                case 5334:
                case 5335:
                case 5336:
                case 5337:
                case 5338:
                case 5339:
                case 5340:
                    {
                        HandlePatchWatering(character, item, patch);
                        return true;
                    }

                case 5329: // secateurs
                case 6036: // plant cure
                    {
                        HandlePatchCuring(character, item, patch);
                        return true;
                    }

                case 6032: // compost
                case 6034: // super compost
                    {
                        HandlePatchCompost(character, item, patch);
                        return true;
                    }

                default:
                    {
                        var farmingManager = character.ServiceProvider.GetRequiredService<IFarmingService>();
                        var seed = await farmingManager.FindSeedById(item.Id);
                        if (seed == null)
                        {
                            return false;
                        }

                        HandlePatchPlanting(character, item, patch, seed);
                        return true;
                    }
            }
        }

        /// <summary>
        ///     Handles the patch planting.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        /// <param name="patch">The patch.</param>
        /// <param name="seed">The seed.</param>
        private void HandlePatchPlanting(ICharacter character, IItem item, IFarmingPatch patch, SeedDto seed)
        {
            if (patch.PatchDefinition.Type != seed.Type)
            {
                character.SendChatMessage("You can't plant " + seed.Type.ToString().ToLower() + " seeds in a " +
                                          patch.PatchDefinition.Type.ToString().ToLower() + " patch.");
                return;
            }

            if (patch.HasCondition(PatchCondition.Planted) || !patch.HasCondition(PatchCondition.Cleared))
            {
                character.SendChatMessage("The patch needs to be cleared in order to plant " + item.Name.ToLower() + "s.");
                return;
            }

            if (!character.Inventory.Contains(5343))
            {
                character.SendChatMessage("You need a seed dibber in order to plant " + item.Name.ToLower() + "s.");
                return;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Farming) < seed.RequiredLevel)
            {
                character.SendChatMessage("You need a farming level of " + seed.RequiredLevel + " to plant " + item.Name.ToLower() + "s.");
                return;
            }

            var count = patch.PatchDefinition.Type == PatchType.Allotment || patch.PatchDefinition.Type == PatchType.Hop ? 3 : 1;
            if (!character.Inventory.Contains(item.Id, count))
            {
                character.SendChatMessage("You need at least " + count + " x " + item.Name.ToLower() + "s in order to plant in the " +
                                          patch.PatchDefinition.Type.ToString().ToLower() + " patch.");
                return;
            }

            character.QueueAnimation(Animation.Create(2291));
            character.SendChatMessage("You plant the " + item.Name.ToLower() + " in the " + patch.PatchDefinition.Type.ToString().ToLower() + " patch.");
            character.Statistics.AddExperience(StatisticsConstants.Farming, seed.PlantingExperience);
            character.Inventory.Remove(_itemBuilder.Create().WithId(item.Id).WithCount(count).Build());
            patch.Plant(seed);
        }

        /// <summary>
        ///     Handles the patch watering.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        /// <param name="patch">The patch.</param>
        private void HandlePatchWatering(ICharacter character, IItem item, IFarmingPatch patch)
        {
            if (!patch.HasCondition(PatchCondition.Planted))
            {
                character.SendChatMessage("You need to plant something first before watering.");
                return;
            }

            if (patch.HasCondition(PatchCondition.Watered))
            {
                character.SendChatMessage("This patch has already been watered.");
                return;
            }

            if (patch.PatchDefinition.Type != PatchType.Allotment && patch.PatchDefinition.Type != PatchType.Hop)
            {
                character.SendChatMessage("This patch doesn't need watering.");
                return;
            }

            if (patch.HasCondition(PatchCondition.Diseased))
            {
                character.SendChatMessage("This crop is diseased and needs to be cured.");
                return;
            }

            if (patch.HasCondition(PatchCondition.Dead))
            {
                character.SendChatMessage("You can't water dead crop!");
                return;
            }

            var itemRepository = character.ServiceProvider.GetRequiredService<IItemService>();
            character.QueueAnimation(Animation.Create(2293));
            character.SendChatMessage("You water the " + itemRepository.FindItemDefinitionById(patch.Seed!.ProductID).Name.ToLower() + " patch.");
            patch.AddCondition(PatchCondition.Watered);
            character.QueueTask(new RsTask(() =>
                {
                    var slot = character.Inventory.GetInstanceSlot(item);
                    if (slot == -1)
                    {
                        return;
                    }

                    var newItemID = item.Id - 1;
                    if (newItemID == 5332)
                    {
                        newItemID = 5331;
                    }

                    character.Inventory.Replace(slot, _itemBuilder.Create().WithId(newItemID).Build());
                    patch.Refresh();
                },
                2));
        }

        /// <summary>
        ///     Handles the patch compost.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        /// <param name="patch">The patch.</param>
        private void HandlePatchCompost(ICharacter character, IItem item, IFarmingPatch patch)
        {
            if (patch.HasCondition(PatchCondition.Fertilized))
            {
                character.SendChatMessage("This patch is saturated by compost.");
                return;
            }

            if (patch.HasCondition(PatchCondition.SuperFertilized))
            {
                character.SendChatMessage("This patch is saturated by supercompost.");
                return;
            }

            if (!patch.HasCondition(PatchCondition.Cleared))
            {
                character.SendChatMessage("The patch needs to be cleared in order to saturate it with compost.");
                return;
            }

            character.QueueAnimation(Animation.Create(2283));
            character.SendChatMessage("You treat the " + patch.PatchDefinition.Type.ToString().ToLower() + " patch with " + item.Name.ToLower() + ".");
            patch.AddCondition(item.Id == 6032 ? PatchCondition.Fertilized : PatchCondition.SuperFertilized);
            character.QueueTask(new RsTask(() =>
                {
                    var slot = character.Inventory.GetInstanceSlot(item);
                    if (slot == -1)
                    {
                        return;
                    }

                    character.Inventory.Replace(slot, _itemBuilder.Create().WithId(_emptyBucketID).Build());
                    character.Statistics.AddExperience(StatisticsConstants.Farming, item.Id == 6032 ? 18 : 26);
                    patch.Refresh();
                },
                2));
        }

        /// <summary>
        ///     Handles the patch curing.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        /// <param name="patch">The patch.</param>
        private void HandlePatchCuring(ICharacter character, IItem? item, IFarmingPatch patch)
        {
            var itemRepository = character.ServiceProvider.GetRequiredService<IItemService>();
            if (!patch.HasCondition(PatchCondition.Cleared))
            {
                character.SendChatMessage("The patch needs weeding.");
                return;
            }

            if (!patch.HasCondition(PatchCondition.Planted))
            {
                character.SendChatMessage("The patch is ready for planting.");
                return;
            }

            if (!patch.HasCondition(PatchCondition.Diseased))
            {
                character.SendChatMessage("The " + itemRepository.FindItemDefinitionById(patch.Seed?.ProductID ?? 0).Name.ToLower() +
                                          " in this patch isn't diseased and doesn't need to be cured.");
                return;
            }

            var cureID = GetCureItemID(patch.PatchDefinition.Type);
            if (!character.Inventory.Contains(cureID))
            {
                character.SendChatMessage("You need a " + itemRepository.FindItemDefinitionById(cureID).Name.ToLower() + " in order to cure this patch.");
                return;
            }

            if (cureID != 6036)
            {
                character.SendChatMessage("You prune the " + patch.PatchDefinition.Type.ToString().ToLower() + "s diseased branches.");
            }
            else
            {
                character.SendChatMessage("You treat the " + patch.PatchDefinition.Type.ToString().ToLower() + " patch with the plant cure.");
            }

            patch.RemoveCondition(PatchCondition.Diseased);
            patch.Reset(); // reset grow time
            character.QueueAnimation(Animation.Create(GetCureAnimationID(patch.PatchDefinition.Type)));
            character.QueueTask(new RsTask(() =>
                {
                    if (cureID == 6036) // plant cure
                    {
                        var slot = item != null
                            ? character.Inventory.GetInstanceSlot(item)
                            : character.Inventory.GetSlotByItem(_itemBuilder.Create().WithId(cureID).Build());
                        if (slot == -1)
                        {
                            return;
                        }

                        character.Inventory.Replace(slot, _itemBuilder.Create().WithId(229).Build());
                    }

                    character.SendChatMessage("The plants in this patch has been restored to its natural health.");
                    patch.Refresh();
                },
                2));
        }

        /// <summary>
        ///     Gets the harvest animation.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private int GetHarvestAnimation(PatchType type)
        {
            switch (type)
            {
                case PatchType.Tree:
                case PatchType.Hop:
                case PatchType.Allotment:
                    return 830;
                case PatchType.Herb: return 2282;
                case PatchType.Flower: return 2292;
                case PatchType.FruitTree: return 2280;
                case PatchType.Bush: return 2281;
                default: return -1;
            }
        }

        /// <summary>
        ///     Gets the cure item identifier.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private int GetCureItemID(PatchType type)
        {
            switch (type)
            {
                case PatchType.FruitTree:
                case PatchType.Tree:
                case PatchType.Bush:
                    return 5329; // secateurs
                default: return 6036; // plant cure
            }
        }

        /// <summary>
        ///     Gets the cure animation identifier.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private int GetCureAnimationID(PatchType type)
        {
            switch (type)
            {
                case PatchType.FruitTree:
                case PatchType.Tree:
                case PatchType.Bush:
                    return 2275; // secateurs anim
                default: return 2288; // plant cure anim
            }
        }
    }
}