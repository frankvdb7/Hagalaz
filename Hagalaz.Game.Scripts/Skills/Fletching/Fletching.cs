using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    /// <summary>
    /// </summary>
    public static class Fletching
    {
        /// <summary>
        ///     The wood
        /// </summary>
        public static readonly FletchingDefinition[] Wood =
        [
            new(1511, 946, [52, 50, 48, 9440], [1, 5, 10, 9], [0.33, 5.0, 10.0, 6.0], [15, 1, 1, 1], 6702), // normal bow
            new(1521, 946, [54, 56, 9442], [20, 25, 24], [16.5, 25.0, 16.0], 6702), // oak bow
            new(1519, 946, [60, 58, 9444], [35, 40, 39], [33.3, 41.5, 22.0], 6702), // willow bow
            new(1517, 946, [64, 62, 9448], [50, 55, 54], [50.0, 58.3, 32.0], 6702), // maple bow
            new(1515, 946, [68, 66, 9452], [65, 70, 69], [67.5, 75.0, 50.0], 6702), // yew bow
            new(1513, 946, [72, 70], [80, 85], [83.25, 91.5], 6702) // magic bow
        ];

        /// <summary>
        ///     The bows
        /// </summary>
        public static readonly FletchingDefinition[] Bows =
        [
            /*
             * Bows
             */ new(50, 1777, [841], [5], [5.0], 6678), // unstrung short bow
            new(48, 1777, [839], [10], [10.0], 6684), // unstrung long bow

            new(54, 1777, [843], [20], [16.5], 6679), // unstrung oak short bow
            new(56, 1777, [845], [25], [25.0], 6685), // unstrung oak long bow

            new(60, 1777, [849], [35], [33.3], 6680), // unstrung willow short bow
            new(58, 1777, [847], [40], [41.5], 6686), // unstrung willow long bow

            new(64, 1777, [853], [50], [50.0], 6681), // unstrung maple short bow
            new(62, 1777, [851], [55], [58.3], 6687), // unstrung maple long bow

            new(68, 1777, [857], [65], [67.5], 6682), // unstrung yew short bow
            new(66, 1777, [855], [70], [75.0], 6688), // unstrung yew long bow

            new(72, 1777, [861], [80], [83.25], 6683), // unstrung magic short bow
            new(70, 1777, [859], [85], [91.5], 6689), // unstrung magic long bow

            /*
             * Crossbows
             */ new(9440, 9420, [9454], [9], [6.0]), // bronze cbow (u)
            new(9442, 9422, [9456], [24], [16.0]), // blurite cbow (u)
            new(9444, 9423, [9457], [39], [22.0]), // iron cbow (u)
            new(9446, 9425, [9459], [46], [27.0]), // steel cbow (u)
            new(9448, 9427, [9461], [54], [32.0]), // mithril cbow (u)
            new(9450, 9429, [9463], [61], [41.0]), // adamant cbow (u)
            new(9452, 9431, [9465], [69], [50.0]), // rune cbow (u)

            new(9454, 9438, [9174], [9], [6.0], 6671), // bronze cbow
            new(9456, 9438, [9176], [24], [16.0], 6672), // blurite cbow
            new(9457, 9438, [9177], [39], [22.0], 6673), // iron cbow
            new(9459, 9438, [9179], [46], [27.0], 6674), // steel cbow
            new(9461, 9438, [9181], [54], [32.0], 6675), // mithril cbow
            new(9463, 9438, [9183], [61], [41.0], 6676), // adamant cbow
            new(9465, 9438, [9185], [69], [50.0], 6677) // rune cbow
        ];

        /// <summary>
        ///     The ammo
        /// </summary>
        public static readonly FletchingDefinition[] Ammo =
        [
            /*
             * Arrows
             */ new(52, 314, [53], [1], [1.0], [15]), // headless arrows
            new(39, 53, [882], [1], [2.6], [15]), // bronze arrows
            new(40, 53, [884], [15], [3.8], [15]), // iron arrows
            new(41, 53, [886], [30], [6.3], [15]), // steel arrows
            new(42, 53, [888], [45], [8.8], [15]), // mithril arrows
            new(43, 53, [890], [60], [10.0], [15]), // adamant arrows
            new(44, 53, [892], [75], [13.8], [15]), // rune arrows
            new(11237, 53, [11212], [90], [16.3], [15]), // dragon arrows

            /*
             * Bolts
             */ new(9375, 314, [877], [9], [0.5], [10]), // bronze bolts
            new(9376, 314, [9139], [24], [1.0], [10]), // blurite bolts
            new(9337, 314, [9140], [39], [1.5], [10]), // iron bolts
            new(9382, 314, [9145], [43], [2.5], [10]), // silver bolts
            new(9339, 314, [9141], [46], [3.5], [10]), // steel bolts
            new(3979, 314, [9142], [54], [5.0], [10]), // mithril bolts
            new(9380, 314, [9143], [61], [7.0], [10]), // adamant bolts
            new(9381, 314, [9144], [69], [10.0], [10]), // rune bolts

            new(45, 877, [879], [11], [1.6], [10]), // opal bolts
            new(9187, 9376, [9335], [26], [2.4], [10]), // jade bolts
            new(46, 9140, [880], [41], [3.2], [10]), // pearl bolts
            new(9188, 9141, [9336], [48], [3.9], [10]), // red topaz bolts
            new(47, 877, [881], [51], [9.5], [10]), // barbed bolts
            new(9189, 9142, [9337], [56], [4.7], [10]), // sapphire bolts
            new(9190, 9376, [9335], [58], [5.5], [10]), // emerald bolts
            new(9191, 9143, [9339], [63], [6.3], [10]), // ruby bolts
            new(9192, 9143, [9340], [65], [7.0], [10]), // diamond bolts
            new(9193, 9144, [9341], [71], [8.2], [10]), // dragon bolts
            new(9194, 9144, [9342], [73], [9.4], [10]), // onyx bolts

            /*
             * Darts
             */ new(819, 314, [806], [1], [18.0], [10]), // bronze darts
            new(820, 314, [807], [22], [32.0], [10]), // iron darts
            new(821, 314, [808], [37], [75.0], [10]), // steel darts
            new(822, 314, [809], [52], [112.0], [10]), // mithril darts
            new(823, 314, [810], [67], [150.0], [10]), // adamant darts
            new(824, 314, [811], [81], [188.0], [10]), // rune darts
            new(11232, 314, [11230], [95], [250.0], [10]), // dragon darts

            /*
             * Misc
             */ new(9416, 954, [9418], [59], [59.0]) // mithril grapple
        ];

        /// <summary>
        ///     The tips
        /// </summary>
        public static readonly FletchingDefinition[] Tips =
        [
            new(1609, 1755, [45], [11], [1.5], [12]), new(1611, 1755, [9187], [26], [2.4], [12]), // jade bolt tips
            new(413, 1755, [46], [41], [3.2], [24]), // 24 pearl bolt tips
            new(411, 1755, [46], [41], [3.2], [6]), // 6 pearl bolt tips
            new(1613, 1755, [9188], [48], [3.9], [12]), // topaz bolt tips
            new(1607, 1755, [9189], [56], [4.7], [12]), // sapphire bolt tips
            new(1605, 1755, [9190], [58], [5.5], [12]), // emerald bolt tips
            new(1603, 1755, [9191], [63], [6.3], [12]), // ruby bolt tips
            new(1601, 1755, [9192], [65], [7.0], [12]), // diamond bolt tips
            new(1615, 1755, [9193], [71], [8.2], [12]), // dragon bolt tips
            new(6573, 1755, [9194], [73], [9.4], [24]) // onyx bolt tips
        ];

        /// <summary>
        ///     Tries the make strung bow.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The item.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public static bool TryFletchWood(ICharacter character, IItem used, IItem usedWith)
        {
            var definitionId = GetWoodDefinitionId(used, usedWith);
            if (definitionId == -1)
            {
                definitionId = GetWoodDefinitionId(usedWith, used);
                if (definitionId == -1)
                {
                    return false;
                }
            }

            // check if the instances do exist, this is merely a check to prevent 'fake' items.
            if (character.Inventory.GetInstanceSlot(used) == -1)
            {
                return false;
            }

            if (character.Inventory.GetInstanceSlot(usedWith) == -1)
            {
                return false;
            }

            var definition = Wood[definitionId];
            var defaultScript = character.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
            character.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.InteractiveChatBox, 0, defaultScript, false);
            var parent = character.Widgets.GetOpenWidget((short)DialogueInterfaces.InteractiveChatBox);
            if (parent == null)
            {
                return false;
            }

            var fletchingDialogue = character.ServiceProvider.GetRequiredService<FletchingDialogue>();
            fletchingDialogue.Definition = definition;
            fletchingDialogue.TickDelay = 3;
            fletchingDialogue.OnFletchingPerformCallback = productIndex =>
            {
                var amount = 1;
                var resource = character.Inventory.GetById(definition.ResourceID);
                if (resource == null)
                {
                    return true;
                }

                var slot = character.Inventory.GetInstanceSlot(resource);
                if (slot == -1)
                {
                    return true;
                }

                var removed = character.Inventory.Remove(resource);
                if (removed <= 0)
                {
                    return true;
                }

                amount = definition.ProductAmounts[productIndex];
                var product = new Item(definition.ProductIDs[productIndex], amount);
                character.Inventory.Add(product);
                if (amount > 1)
                {
                    character.SendChatMessage("You carefully cut the wood into " + amount + " " + product.Name.ToLower() + "s.");
                }
                else
                {
                    character.SendChatMessage("You carefully cut the wood into a " + product.Name.ToLower() + ".");
                }

                character.Statistics.AddExperience(StatisticsConstants.Fletching, definition.Experience[productIndex] * amount);
                return false;
            };
            character.Widgets.OpenWidget((short)DialogueInterfaces.InteractiveSelectAmountBox, parent, 4, 0, fletchingDialogue, false);
            return true;
        }

        /// <summary>
        ///     Tries the fletch a bow (u) to a bow.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public static bool TryFletchBow(ICharacter character, IItem used, IItem usedWith)
        {
            var definitionId = GetBowDefinitionId(used, usedWith);
            if (definitionId == -1)
            {
                definitionId = GetBowDefinitionId(usedWith, used);
                if (definitionId == -1)
                {
                    return false;
                }
            }

            // check if the instances do exist, this is merely a check to prevent 'fake' items.
            if (character.Inventory.GetInstanceSlot(used) == -1)
            {
                return false;
            }

            if (character.Inventory.GetInstanceSlot(usedWith) == -1)
            {
                return false;
            }

            var definition = Bows[definitionId];

            var defaultScript = character.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
            character.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.InteractiveChatBox, 0, defaultScript, false);
            var parent = character.Widgets.GetOpenWidget((short)DialogueInterfaces.InteractiveChatBox);
            if (parent == null)
            {
                return false;
            }

            var fletchingDialogue = character.ServiceProvider.GetRequiredService<FletchingDialogue>();
            fletchingDialogue.Definition = definition;
            fletchingDialogue.TickDelay = 2;
            fletchingDialogue.OnFletchingPerformCallback = productIndex =>
            {
                var resource = character.Inventory.GetById(definition.ResourceID);
                if (resource == null)
                {
                    return true;
                }

                var resourceSlot = character.Inventory.GetInstanceSlot(resource);
                if (resourceSlot == -1)
                {
                    return true;
                }

                var tool = character.Inventory.GetById(definition.ToolID);
                if (tool == null)
                {
                    return true;
                }

                var toolSlot = character.Inventory.GetInstanceSlot(tool);
                if (toolSlot == -1)
                {
                    return true;
                }

                var removed = character.Inventory.Remove(resource, resourceSlot);
                if (removed <= 0 || removed > int.MaxValue)
                {
                    return true;
                }

                removed = character.Inventory.Remove(tool, toolSlot);
                if (removed <= 0 || removed > int.MaxValue)
                {
                    return true;
                }

                var product = new Item(definition.ProductIDs[productIndex]);
                character.Inventory.Add(resourceSlot, product);
                character.SendChatMessage("You add a " + tool.Name.ToLower() + " to the " + product.Name.ToLower() + ".");
                character.Statistics.AddExperience(StatisticsConstants.Fletching, definition.Experience[productIndex]);
                return false;
            };
            character.Widgets.OpenWidget((short)DialogueInterfaces.InteractiveSelectAmountBox, parent, 4, 0, fletchingDialogue, false);
            return true;
        }

        /// <summary>
        ///     Tries the fletch a bow (u) to a bow.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public static bool TryFletchAmmo(ICharacter character, IItem used, IItem usedWith)
        {
            var definitionId = GetAmmoDefinitionId(used, usedWith);
            if (definitionId == -1)
            {
                definitionId = GetAmmoDefinitionId(usedWith, used);
                if (definitionId == -1)
                {
                    return false;
                }
            }

            // check if the instances do exist, this is merely a check to prevent 'fake' items.
            if (character.Inventory.GetInstanceSlot(used) == -1)
            {
                return false;
            }

            if (character.Inventory.GetInstanceSlot(usedWith) == -1)
            {
                return false;
            }

            var definition = Ammo[definitionId];

            var defaultScript = character.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
            character.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.InteractiveChatBox, 0, defaultScript, false);
            var parent = character.Widgets.GetOpenWidget((short)DialogueInterfaces.InteractiveChatBox);
            if (parent == null)
            {
                return false;
            }

            var fletchingDialogue = character.ServiceProvider.GetRequiredService<FletchingDialogue>();
            fletchingDialogue.Definition = definition;
            fletchingDialogue.TickDelay = 1;
            fletchingDialogue.OnFletchingPerformCallback = productIndex =>
            {
                var resource = character.Inventory.GetById(definition.ResourceID);
                if (resource == null)
                {
                    return true;
                }

                var resourceSlot = character.Inventory.GetInstanceSlot(resource);
                if (resourceSlot == -1)
                {
                    return true;
                }

                var tool = character.Inventory.GetById(definition.ToolID);
                if (tool == null)
                {
                    return true;
                }

                var toolSlot = character.Inventory.GetInstanceSlot(tool);
                if (toolSlot == -1)
                {
                    return true;
                }

                var amount = definition.ProductAmounts[productIndex];

                var itemAmount = character.Inventory.GetCount(resource);
                if (itemAmount < amount)
                {
                    amount = itemAmount;
                }

                itemAmount = character.Inventory.GetCount(tool);
                if (itemAmount < amount)
                {
                    amount = itemAmount;
                }

                var removed = character.Inventory.Remove(new Item(resource.Id, amount), resourceSlot);
                if (removed < amount)
                {
                    amount = removed;
                }

                removed = character.Inventory.Remove(new Item(tool.Id, amount), toolSlot);
                if (removed < amount)
                {
                    amount = removed;
                }

                if (amount < 0 || amount > int.MaxValue)
                {
                    return true;
                }

                var product = new Item(definition.ProductIDs[productIndex], amount);
                character.Inventory.Add(product);
                character.SendChatMessage("You attach " + tool.Name.ToLower() + " to the " + product.Name.ToLower() + ".");
                character.Statistics.AddExperience(StatisticsConstants.Fletching, definition.Experience[productIndex] * amount);
                return false;
            };

            character.Widgets.OpenWidget((short)DialogueInterfaces.InteractiveSelectAmountBox, parent, 4, 0, fletchingDialogue, false);
            return true;
        }

        /// <summary>
        ///     Tries the fletch tips.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public static bool TryFletchTips(ICharacter character, IItem used, IItem usedWith)
        {
            var definitionId = GetTipsDefinitionId(used, usedWith);
            if (definitionId == -1)
            {
                definitionId = GetTipsDefinitionId(usedWith, used);
                if (definitionId == -1)
                {
                    return false;
                }
            }

            // check if the instances do exist, this is merely a check to prevent 'fake' items.
            if (character.Inventory.GetInstanceSlot(used) == -1)
            {
                return false;
            }

            if (character.Inventory.GetInstanceSlot(usedWith) == -1)
            {
                return false;
            }

            var definition = Tips[definitionId];
            var defaultScript = character.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
            character.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.InteractiveChatBox, 0, defaultScript, false);
            var parent = character.Widgets.GetOpenWidget((short)DialogueInterfaces.InteractiveChatBox);
            if (parent == null)
            {
                return false;
            }

            var fletchingDialogue = character.ServiceProvider.GetRequiredService<FletchingDialogue>();
            fletchingDialogue.Definition = definition;
            fletchingDialogue.TickDelay = 3;
            fletchingDialogue.OnFletchingPerformCallback = productIndex =>
            {
                var resource = character.Inventory.GetById(definition.ResourceID);
                if (resource == null)
                {
                    return true;
                }

                var resourceSlot = character.Inventory.GetInstanceSlot(resource);
                if (resourceSlot == -1)
                {
                    return true;
                }

                var tool = character.Inventory.GetById(definition.ToolID);
                if (tool == null)
                {
                    return true;
                }

                var amount = definition.ProductAmounts[productIndex];

                var removed = character.Inventory.Remove(new Item(resource.Id), resourceSlot);
                if (removed <= 0)
                {
                    return true;
                }

                if (amount < 0 || amount > int.MaxValue)
                {
                    return true;
                }

                var product = new Item(definition.ProductIDs[productIndex], amount);
                character.Inventory.Add(product);
                character.SendChatMessage("You cut the " + resource.Name.ToLower() + " to " + amount + " x " + product.Name.ToLower() + ".");
                character.Statistics.AddExperience(StatisticsConstants.Fletching, definition.Experience[productIndex] * amount);
                return false;
            };

            var contextAccessor = character.ServiceProvider.GetRequiredService<ICharacterContextAccessor>();
            var itemService = character.ServiceProvider.GetRequiredService<IItemService>();
            character.Widgets.OpenWidget((int)DialogueInterfaces.InteractiveSelectAmountBox,
                parent,
                4,
                0,
                fletchingDialogue,
                false);
            return true;
        }

        /// <summary>
        ///     Tries to start fletching.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="onFletchingPerformCallback">The on fletching perform callback.</param>
        /// <param name="productIndex">Index of the product.</param>
        /// <param name="tickDelay">The tick delay.</param>
        /// <param name="count">The count.</param>
        public static void TryStartFletching(
            ICharacter character, FletchingDefinition definition, Func<int, bool> onFletchingPerformCallback, int productIndex, int tickDelay, int count)
        {
            var levelRequired = definition.RequiredLevels[productIndex];
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Fletching) < levelRequired)
            {
                character.SendChatMessage("You need a fletching level of " + levelRequired + " in order to fletch this.");
                return;
            }

            character.QueueTask(new FletchingTask(character, definition, productIndex, count, onFletchingPerformCallback, tickDelay));
        }

        /// <summary>
        ///     Gets the bow(u) definition id.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public static int GetBowDefinitionId(IItem used, IItem usedWith)
        {
            for (var i = 0; i < Bows.Length; i++)
            {
                if (used.Id == Bows[i].ResourceID && usedWith.Id == Bows[i].ToolID || usedWith.Id == Bows[i].ResourceID && used.Id == Bows[i].ToolID)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Gets the wood definition identifier.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public static int GetWoodDefinitionId(IItem used, IItem usedWith)
        {
            for (var i = 0; i < Wood.Length; i++)
            {
                if (used.Id == Wood[i].ResourceID && usedWith.Id == Wood[i].ToolID || usedWith.Id == Wood[i].ResourceID && used.Id == Wood[i].ToolID)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Gets the ammo definition identifier.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public static int GetAmmoDefinitionId(IItem used, IItem usedWith)
        {
            for (var i = 0; i < Ammo.Length; i++)
            {
                if (used.Id == Ammo[i].ResourceID && usedWith.Id == Ammo[i].ToolID || usedWith.Id == Ammo[i].ResourceID && used.Id == Ammo[i].ToolID)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Gets the tips definition identifier.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        public static int GetTipsDefinitionId(IItem used, IItem usedWith)
        {
            for (var i = 0; i < Tips.Length; i++)
            {
                if (used.Id == Tips[i].ResourceID && usedWith.Id == Tips[i].ToolID || usedWith.Id == Tips[i].ResourceID && used.Id == Tips[i].ToolID)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}