using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Scripts.Dialogues.Generic;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    /// <summary>
    /// </summary>
    public class CraftingSkillService : ICraftingSkillService
    {
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        ///     The gold bar.
        /// </summary>
        public const int GoldBar = 2357;

        /// <summary>
        ///     The silver bar.
        /// </summary>
        public const int SilverBar = 2355;

        /// <summary>
        ///     The ring mould.
        /// </summary>
        public const int RingMoldId = 1592;

        /// <summary>
        ///     The amulet mould identifier
        /// </summary>
        public const int AmuletMouldId = 1595;

        /// <summary>
        ///     The necklace mould identifier.
        /// </summary>
        public const int NecklaceMouldId = 1597;

        /// <summary>
        ///     The bracelet mould identifier.
        /// </summary>
        public const int BraceletMouldId = 11065;

        /// <summary>
        ///     The un holy mould identifier.
        /// </summary>
        public static readonly int UnHolyMouldId = 1594;

        /// <summary>
        ///     The holy mould identifier.
        /// </summary>
        public static readonly int HolyMouldId = 1599;

        /// <summary>
        ///     The conductor mould identifier.
        /// </summary>
        public static readonly int ConductorMouldId = 4200;

        /// <summary>
        ///     The sickly mould identifier.
        /// </summary>
        public static readonly int SicklyMouldId = 2976;

        /// <summary>
        ///     The bolt mould identifier.
        /// </summary>
        public static readonly int BoltMouldId = 9434;

        /// <summary>
        ///     The no ring.
        /// </summary>
        public const int NoRingId = 1647;

        /// <summary>
        ///     The no necklace identifier.
        /// </summary>
        public const int NoNecklaceId = 1666;

        /// <summary>
        ///     The no amulet identifier.
        /// </summary>
        public const int NoAmuletId = 1685;

        /// <summary>
        ///     The no bracelet identifier.
        /// </summary>
        public const int NoBraceletId = 11067;

        /// <summary>
        ///     The needle identifier
        /// </summary>
        public const int NeedleId = 1733;

        /// <summary>
        ///     The thread identifier
        /// </summary>
        public const int ThreadId = 1734;

        /// <summary>
        ///     The soft clay identifier
        /// </summary>
        public const int SoftClayId = 1761;

        public CraftingSkillService(IItemBuilder itemBuilder)
        {
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Tries the bake pottery.
        /// </summary>
        /// <param name="character">The character.</param>
        public async Task TryBakePottery(ICharacter character)
        {
            var craftingManager = character.ServiceProvider.GetRequiredService<ICraftingService>();
            var itemManager = character.ServiceProvider.GetRequiredService<IItemService>();
            var productIDs = (await craftingManager.FindAllPottery()).Select(p => p.BakedProductID).ToArray();

            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = productIDs;
            dialogue.PerformMakeProductCallback =
                (productID, count) => BakePottery(character, craftingManager.FindPotteryByFinalProductID(productID).Result, count);
            dialogue.ProductNamingCallback = productID =>
            {
                var definition = craftingManager.FindPotteryByFinalProductID(productID).Result;
                if (definition == null)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder();
                var hasRequirements = character.Inventory.Contains(definition.FormedProductID)
                                      && character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) >= definition.RequiredLevel;
                if (!hasRequirements)
                {
                    builder.Append("<col=FF0000>");
                }

                builder.Append(itemManager.FindItemDefinitionById(productID).Name);
                if (!hasRequirements)
                {
                    builder.Append("</col>");
                }

                return builder.ToString();
            };
            dialogue.SetMaxCount(character.Inventory.Capacity, false);
            dialogue.SetCurrentCount(character.Inventory.Capacity, false);
            InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }

        /// <summary>
        ///     Bakes the pottery.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private bool BakePottery(ICharacter character, PotteryDto? definition, int count)
        {
            if (definition == null)
            {
                return false;
            }

            var itemRepository = character.ServiceProvider.GetRequiredService<IItemService>();
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) < definition.RequiredLevel)
            {
                character.SendChatMessage("You need a Crafting level of at least " + definition.RequiredLevel + " to bake a " +
                                          itemRepository.FindItemDefinitionById(definition.FormedProductID).Name + ".");
                return false;
            }

            MakeTask task = null;
            task = new MakeTask(count,
                () =>
                {
                    if (task.CurrentMakeCount == task.TotalMakeCount)
                    {
                        task.Cancel();
                        return;
                    }

                    if (task.TickCount == 1 || task.TickCount % 6 == 0)
                    {
                        if (!character.Inventory.Contains(definition.FormedProductID))
                        {
                            character.SendChatMessage("You need " + itemRepository.FindItemDefinitionById(definition.FormedProductID).Name.ToLower() +
                                                      " to create a " + itemRepository.FindItemDefinitionById(definition.BakedProductID).Name.ToLower() + ".");
                            task.Cancel();
                            return;
                        }

                        character.QueueAnimation(Animation.Create(897));
                        return;
                    }

                    if (task.TickCount % 3 == 0)
                    {
                        task.CurrentMakeCount++;
                        var resource = character.Inventory.GetById(definition.FormedProductID);
                        if (resource == null)
                        {
                            task.Cancel();
                            return;
                        }

                        var slot = character.Inventory.GetInstanceSlot(resource);
                        if (slot == -1)
                        {
                            task.Cancel();
                            return;
                        }

                        character.Inventory.Replace(slot, _itemBuilder.Create().WithId(definition.BakedProductID).Build());
                        character.Statistics.AddExperience(StatisticsConstants.Crafting, definition.BakeExperience);
                    }
                });
            character.QueueTask(task);
            return true;
        }

        /// <summary>
        ///     Tries the form pottery.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public async Task TryFormPottery(ICharacter character)
        {
            var craftingManager = character.ServiceProvider.GetRequiredService<ICraftingService>();
            var itemManager = character.ServiceProvider.GetRequiredService<IItemService>();
            var productIDs = (await craftingManager.FindAllPottery()).Select(p => p.FormedProductID).ToArray();

            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = productIDs;
            dialogue.PerformMakeProductCallback = (productID, count) => FormPottery(character, craftingManager.FindPotteryByProductID(productID).Result, count);
            dialogue.ProductNamingCallback = productID =>
            {
                var definition = craftingManager.FindPotteryByProductID(productID).Result;
                if (definition == null)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder();
                var hasRequirements = character.Inventory.Contains(SoftClayId)
                                      && character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) >= definition.RequiredLevel;
                if (!hasRequirements)
                {
                    builder.Append("<col=FF0000>");
                }

                builder.Append(itemManager.FindItemDefinitionById(productID).Name);
                if (!hasRequirements)
                {
                    builder.Append("</col>");
                }

                return builder.ToString();
            };

            var resCount = character.Inventory.GetCountById(SoftClayId);
            dialogue.SetMaxCount(resCount, false);
            dialogue.SetCurrentCount(resCount, false);
            InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }

        /// <summary>
        ///     Forms the pottery.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private bool FormPottery(ICharacter character, PotteryDto? definition, int count)
        {
            var itemRepository = character.ServiceProvider.GetRequiredService<IItemService>();
            if (definition == null)
            {
                return false;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) < definition.RequiredLevel)
            {
                character.SendChatMessage("You need a Crafting level of at least " + definition.RequiredLevel + " to form a " +
                                          itemRepository.FindItemDefinitionById(definition.FormedProductID).Name + ".");
                return false;
            }

            MakeTask task = null;
            task = new MakeTask(count,
                () =>
                {
                    if (task.CurrentMakeCount == task.TotalMakeCount)
                    {
                        task.Cancel();
                        return;
                    }

                    if (task.TickCount == 1 || task.TickCount % 6 == 0)
                    {
                        if (!character.Inventory.Contains(SoftClayId))
                        {
                            character.SendChatMessage("You need " + itemRepository.FindItemDefinitionById(SoftClayId).Name.ToLower() + " to create a " +
                                                      itemRepository.FindItemDefinitionById(definition.FormedProductID).Name.ToLower() + ".");
                            task.Cancel();
                            return; // stop
                        }

                        character.QueueAnimation(Animation.Create(897));
                        return;
                    }

                    if (task.TickCount % 3 == 0)
                    {
                        task.CurrentMakeCount++;
                        var resource = character.Inventory.GetById(SoftClayId);
                        if (resource == null)
                        {
                            task.Cancel();
                            return;
                        }

                        var slot = character.Inventory.GetInstanceSlot(resource);
                        if (slot == -1)
                        {
                            task.Cancel();
                            return;
                        }

                        character.Inventory.Replace(slot, _itemBuilder.Create().WithId(definition.FormedProductID).Build());
                        character.Statistics.AddExperience(StatisticsConstants.Crafting, definition.FormExperience);
                    }
                });
            character.QueueTask(task);
            return true;
        }

        /// <summary>
        ///     Cuts the gem.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="uncut">The uncut.</param>
        public async Task<bool> TryCutGem(ICharacter character, IItem uncut)
        {
            var craftingManager = character.ServiceProvider.GetRequiredService<ICraftingService>();
            var definition = await craftingManager.FindGemByResourceID(uncut.Id);
            if (definition == null)
            {
                return false;
            }

            var itemManager = character.ServiceProvider.GetRequiredService<IItemService>();
            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = [definition.CutGemID];
            dialogue.PerformMakeProductCallback = (productID, count) => CutGem(character, definition, count);
            dialogue.ProductNamingCallback = productID =>
            {
                var builder = new StringBuilder();
                var hasRequirements = character.Inventory.Contains(definition.UncutGemID)
                                      && character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) >= definition.RequiredLevel;
                if (!hasRequirements)
                {
                    builder.Append("<col=FF0000>");
                }

                builder.Append(itemManager.FindItemDefinitionById(productID).Name);
                if (!hasRequirements)
                {
                    builder.Append("</col>");
                }

                return builder.ToString();
            };
            var resCount = character.Inventory.GetCountById(definition.UncutGemID);
            dialogue.SetMaxCount(resCount, false);
            dialogue.SetCurrentCount(resCount, false);
            return InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }

        /// <summary>
        ///     Cuts the specified gem.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private bool CutGem(ICharacter character, GemDto definition, int count)
        {
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) < definition.RequiredLevel)
            {
                character.SendChatMessage("You need a Crafting level of at least " + definition.RequiredLevel + " to cut this gem.");
                return false;
            }

            character.QueueTask(new CutGemTask(character, definition, count));
            return true;
        }

        /// <summary>
        ///     Tries the spin.
        /// </summary>
        /// <param name="character">The character.</param>
        public async Task TrySpin(ICharacter character)
        {
            var craftingManager = character.ServiceProvider.GetRequiredService<ICraftingService>();
            var productIDs = (await craftingManager.FindAllSpin()).Select(s => s.ProductID).ToArray();
            var itemManager = character.ServiceProvider.GetRequiredService<IItemService>();
            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = productIDs;
            dialogue.PerformMakeProductCallback = (productID, count) => Spin(character, craftingManager.FindSpinByProductID(productID).Result, count);
            dialogue.ProductNamingCallback = productID =>
            {
                var definition = craftingManager.FindSpinByProductID(productID).Result;
                if (definition == null)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder();
                var hasRequirements = character.Inventory.Contains(definition.ResourceID)
                                      && character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) >= definition.RequiredLevel;
                if (!hasRequirements)
                {
                    builder.Append("<col=FF0000>");
                }

                builder.Append(itemManager.FindItemDefinitionById(productID).Name);
                if (!hasRequirements)
                {
                    builder.Append("</col>");
                }

                builder.Append("<br>");
                builder.Append('(').Append(itemManager.FindItemDefinitionById(definition.ResourceID).Name).Append(')');
                return builder.ToString();
            };
            dialogue.SetMaxCount(character.Inventory.Capacity, false);
            dialogue.SetCurrentCount(character.Inventory.Capacity, false);
            InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }

        /// <summary>
        ///     Spins the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="count">The count.</param>
        private bool Spin(ICharacter character, SpinDto? definition, int count)
        {
            var itemRepository = character.ServiceProvider.GetRequiredService<IItemService>();
            if (definition == null)
            {
                return false;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) < definition.RequiredLevel)
            {
                character.SendChatMessage("You need a Crafting level of at least " + definition.RequiredLevel + " to make " +
                                          itemRepository.FindItemDefinitionById(definition.ProductID).Name.ToLower() + ".");
                return false;
            }

            character.QueueTask(new SpinTask(character, definition, count));
            return true;
        }

        /// <summary>
        ///     Tries the craft leather.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="resource">The resource.</param>
        /// <returns></returns>
        public async Task<bool> TryCraftLeather(ICharacter character, IItem resource)
        {
            var craftingManager = character.ServiceProvider.GetRequiredService<ICraftingService>();
            var productIds = (await craftingManager.FindAllLeather()).Where(e => e.ResourceID == resource.Id).Select(e => e.ProductId).ToHashSet().ToArray();
            var itemManager = character.ServiceProvider.GetRequiredService<IItemService>();
            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = productIds;
            dialogue.PerformMakeProductCallback =
                (productID, count) => CraftLeather(character, craftingManager.FindLeatherByProductId(productID).Result, count);
            dialogue.ProductNamingCallback = productID =>
            {
                var definition = craftingManager.FindLeatherByProductId(productID).Result;
                if (definition == null)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder();
                var hasRequirements = character.Inventory.Contains(definition.ResourceID, definition.RequiredResourceCount)
                                      && character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) >= definition.RequiredLevel;
                if (!hasRequirements)
                {
                    builder.Append("<col=FF0000>");
                }

                builder.Append(itemManager.FindItemDefinitionById(productID).Name);
                if (!hasRequirements)
                {
                    builder.Append("</col>");
                }

                return builder.ToString();
            };
            var resCount = character.Inventory.GetCountById(resource.Id);
            dialogue.SetMaxCount(resCount, false);
            dialogue.SetCurrentCount(resCount, false);
            return InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }

        /// <summary>
        ///     Crafts the leather.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="count">The count.</param>
        private bool CraftLeather(ICharacter character, LeatherDto? definition, int count)
        {
            var itemRepository = character.ServiceProvider.GetRequiredService<IItemService>();
            if (definition == null)
            {
                return false;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) < definition.RequiredLevel)
            {
                character.SendChatMessage("You need a Crafting level of at least " + definition.RequiredLevel + " to make " +
                                          itemRepository.FindItemDefinitionById(definition.ProductId).Name.ToLower() + ".");
                return false;
            }

            character.QueueTask(new LeatherTask(character, definition, count));
            return true;
        }

        /// <summary>
        ///     Tries the tan.
        /// </summary>
        /// <param name="character">The character.</param>
        public async Task TryTan(ICharacter character)
        {
            var craftingManager = character.ServiceProvider.GetRequiredService<ICraftingService>();
            var productIDs = (await craftingManager.FindAllTan()).Select(t => t.ProductID).ToArray();
            var itemManager = character.ServiceProvider.GetRequiredService<IItemService>();
            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = productIDs;
            dialogue.PerformMakeProductCallback = (productID, count) => Tan(character, craftingManager.FindTanByProductID(productID).Result, count);
            dialogue.ProductNamingCallback = productID =>
            {
                var definition = craftingManager.FindTanByProductID(productID).Result;
                if (definition == null)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder();
                var hasResource = character.Inventory.Contains(definition.ResourceID);
                if (!hasResource)
                {
                    builder.Append("<col=FF0000>");
                }

                builder.Append(itemManager.FindItemDefinitionById(productID).Name);
                if (!hasResource)
                {
                    builder.Append("</col>");
                }

                builder.Append("<br>");
                builder.Append('(').Append(definition.BasePrice).Append(" coins)");
                return builder.ToString();
            };
            dialogue.SetMaxCount(character.Inventory.Capacity, false);
            dialogue.SetCurrentCount(character.Inventory.Capacity, false);
            InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }

        /// <summary>
        ///     Tans the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private bool Tan(ICharacter character, TanDto? definition, int count)
        {
            if (definition == null)
            {
                return false;
            }

            var resource = new Item(definition.ResourceID);
            if (count <= 0 || character.Inventory.GetCountById(definition.ResourceID) <= 0)
            {
                character.SendChatMessage("You need " + resource.Name + " in order to tan this item.");
                return false;
            }

            var coinCount = (ulong)count * (ulong)definition.BasePrice;
            if (coinCount > int.MaxValue)
            {
                coinCount = int.MaxValue;
            }

            var coinRemoved = character.MoneyPouch.Remove((int)coinCount);
            var tannedCount = coinRemoved / definition.BasePrice;
            var removedCount = character.Inventory.Remove(_itemBuilder.Create().WithId(definition.ResourceID).WithCount(tannedCount).Build());
            if (removedCount > 0)
            {
                var product = _itemBuilder.Create().WithId(definition.ProductID).WithCount(removedCount).Build();
                character.Inventory.Add(product);
                character.SendChatMessage("The tanner tans " + removedCount + " " + product.Name.ToLower() + "s for you.");
                return true;
            }

            return false;
        }
    }
}