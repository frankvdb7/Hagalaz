using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Dialogues.Generic;

namespace Hagalaz.Game.Scripts.Skills.Smithing
{
    /// <summary>
    /// </summary>
    public static class Smithing
    {
        /// <summary>
        ///     The bars.
        /// </summary>
        public static readonly SmithingDefinition[] Bars =
        [
            new(2349,
                new SmeltingBarDefinition([new ItemDto(436), new ItemDto(438)], 1, 6.2),
                new ForgingBarDefinition(12.5,
                [
                    new ForgingBarEntry(new ItemDto(1205), 1, 1), // dagger
                    new ForgingBarEntry(new ItemDto(1351), 1, 1), // hatchet
                    new ForgingBarEntry(new ItemDto(1422), 2, 1), // mace
                    new ForgingBarEntry(new ItemDto(1139), 1, 1), // med helm
                    new ForgingBarEntry(new ItemDto(9375, 10), 3, 1), // bolts
                    new ForgingBarEntry(new ItemDto(1277), 4, 1), // short sword
                    new ForgingBarEntry(new ItemDto(819, 10), 4, 1), // dart tip
                    new ForgingBarEntry(new ItemDto(4819, 15), 4, 1), // nails
                    new ForgingBarEntry(new ItemDto(1794), 4, 1), // wire
                    new ForgingBarEntry(), // empty
                    new ForgingBarEntry(), // empty
                    new ForgingBarEntry(new ItemDto(39, 15), 5, 1), // arrowtips
                    new ForgingBarEntry(new ItemDto(1321), 5, 2), // scimitar
                    new ForgingBarEntry(new ItemDto(9420), 6, 1), // crossbow limbs
                    new ForgingBarEntry(new ItemDto(1291), 6, 2), // longsword
                    new ForgingBarEntry(new ItemDto(864, 5), 7, 1), // throwing knives
                    new ForgingBarEntry(new ItemDto(1155), 7, 2), // full helm
                    new ForgingBarEntry(new ItemDto(1173), 8, 2), // square shield
                    new ForgingBarEntry(), // empty
                    new ForgingBarEntry(new ItemDto(1265), 5, 2), // pickaxe
                    new ForgingBarEntry(new ItemDto(1337), 9, 3), // warhammer
                    new ForgingBarEntry(new ItemDto(1375), 10, 3), // battleaxe
                    new ForgingBarEntry(new ItemDto(1103), 11, 3), // chainbody
                    new ForgingBarEntry(new ItemDto(1189), 12, 3), // kiteshield
                    new ForgingBarEntry(new ItemDto(3095), 13, 2), // claws
                    new ForgingBarEntry(new ItemDto(1307), 14, 3), // 2hand
                    new ForgingBarEntry(new ItemDto(1087), 16, 3), // skirt
                    new ForgingBarEntry(new ItemDto(1075), 16, 3), // legs
                    new ForgingBarEntry(new ItemDto(1117), 18, 5) // platebody
                ])), // bronze bar
            new(9467,
                new SmeltingBarDefinition([new ItemDto(668)], 8, 8.0),
                new ForgingBarDefinition(17.0, [])), // blurite bar
            new(2351,
                new SmeltingBarDefinition([new ItemDto(440)], 15, 12.5),
                new ForgingBarDefinition(25.0,
                [
                    new ForgingBarEntry(new ItemDto(1203), 15, 1), // dagger
                    new ForgingBarEntry(new ItemDto(1349), 16, 1), // hatchet
                    new ForgingBarEntry(new ItemDto(1420), 17, 1), // mace
                    new ForgingBarEntry(new ItemDto(1137), 18, 1), // med
                    new ForgingBarEntry(new ItemDto(9377, 10), 18, 1), // bolts
                    new ForgingBarEntry(new ItemDto(1279), 19, 1), // sword
                    new ForgingBarEntry(new ItemDto(820, 10), 19, 1), // dart tip
                    new ForgingBarEntry(new ItemDto(4820, 15), 19, 1), // nails
                    new ForgingBarEntry(new ItemDto(7225), 17, 1), // spit
                    new ForgingBarEntry(), // empty
                    new ForgingBarEntry(), // empty
                    new ForgingBarEntry(new ItemDto(40, 15), 20, 1), // arrow tips
                    new ForgingBarEntry(new ItemDto(1323), 20, 2), // scimitar
                    new ForgingBarEntry(new ItemDto(9423), 23, 1), // limbs
                    new ForgingBarEntry(new ItemDto(1293), 21, 2), // longsword
                    new ForgingBarEntry(new ItemDto(863, 5), 22, 1), // throwing knives
                    new ForgingBarEntry(new ItemDto(1153), 22, 2), // full helm
                    new ForgingBarEntry(new ItemDto(1175), 23, 2), // square shield
                    new ForgingBarEntry(new ItemDto(4540), 26, 1), // oil latern frame
                    new ForgingBarEntry(new ItemDto(1267), 20, 2), // pickaxe
                    new ForgingBarEntry(new ItemDto(1335), 24, 3), // warhammer
                    new ForgingBarEntry(new ItemDto(1363), 24, 3), // battleaxe
                    new ForgingBarEntry(new ItemDto(1101), 26, 3), // chainbody
                    new ForgingBarEntry(new ItemDto(1191), 27, 3), // kiteshield
                    new ForgingBarEntry(new ItemDto(3096), 28, 2), // claws
                    new ForgingBarEntry(new ItemDto(1309), 29, 3), // 2-handed sword
                    new ForgingBarEntry(new ItemDto(1081), 31, 3), // skirt
                    new ForgingBarEntry(new ItemDto(1067), 31, 3), // legs
                    new ForgingBarEntry(new ItemDto(1115), 33, 5) // platebody
                ])), // iron bar
            new(2355, new SmeltingBarDefinition([new ItemDto(442)], 20, 13.7), new ForgingBarDefinition()), // silver bar
            new(2353,
                new SmeltingBarDefinition([new ItemDto(440), new ItemDto(453, 2)], 30, 17.5),
                new ForgingBarDefinition(37.0,
                [
                    new ForgingBarEntry(new ItemDto(1207), 30, 1), // dagger
                    new ForgingBarEntry(new ItemDto(1353), 31, 1), // hatchet
                    new ForgingBarEntry(new ItemDto(1424), 32, 1), // mace
                    new ForgingBarEntry(new ItemDto(1141), 33, 1), // med helm
                    new ForgingBarEntry(new ItemDto(9378, 10), 33, 1), // crossbow bolts
                    new ForgingBarEntry(new ItemDto(1281), 34, 1), // short sword
                    new ForgingBarEntry(new ItemDto(821, 10), 34, 1), // dart tips
                    new ForgingBarEntry(new ItemDto(1539, 15), 34, 1), // nails
                    new ForgingBarEntry(), // empty
                    new ForgingBarEntry(), // empty
                    new ForgingBarEntry(new ItemDto(2370), 36, 1), // studs
                    new ForgingBarEntry(new ItemDto(41, 15), 35, 1), // arrow tips
                    new ForgingBarEntry(new ItemDto(1325), 35, 2), // scimitar
                    new ForgingBarEntry(new ItemDto(9425), 36, 1), // limbs
                    new ForgingBarEntry(new ItemDto(1295), 36, 2), // long sword
                    new ForgingBarEntry(new ItemDto(865, 5), 37, 1), // throwing knives
                    new ForgingBarEntry(new ItemDto(1157), 37, 2), // full helm
                    new ForgingBarEntry(new ItemDto(1177), 38, 2), // sq shield
                    new ForgingBarEntry(new ItemDto(4544), 49, 1), // bullseye lantern frame
                    new ForgingBarEntry(new ItemDto(1269), 35, 2), // pickaxe
                    new ForgingBarEntry(new ItemDto(1339), 39, 3), // warhammer
                    new ForgingBarEntry(new ItemDto(1365), 40, 3), // battleaxe
                    new ForgingBarEntry(new ItemDto(1105), 41, 3), // chain body
                    new ForgingBarEntry(new ItemDto(1193), 42, 3), // kite shield
                    new ForgingBarEntry(new ItemDto(3097), 43, 2), // claws
                    new ForgingBarEntry(new ItemDto(1311), 44, 3), // 2-handed sword
                    new ForgingBarEntry(new ItemDto(1083), 46, 3), // plateskirt
                    new ForgingBarEntry(new ItemDto(1069), 46, 3), // platelegs
                    new ForgingBarEntry(new ItemDto(1119), 48, 5) // platebody
                ])), // steel bar
            new(2357, new SmeltingBarDefinition([new ItemDto(444)], 40, 22.5), new ForgingBarDefinition()), // gold bar
            new(2359,
                new SmeltingBarDefinition([new ItemDto(447), new ItemDto(453, 4)], 50, 30.0),
                new ForgingBarDefinition(50.0,
                [
                    new ForgingBarEntry(new ItemDto(1209), 50, 1), // dagger
                    new ForgingBarEntry(new ItemDto(1355), 51, 1), // hatchet
                    new ForgingBarEntry(new ItemDto(1428), 52, 1), // mace
                    new ForgingBarEntry(new ItemDto(1143), 53, 1), // med helm
                    new ForgingBarEntry(new ItemDto(9379, 10), 53, 1), // bolts
                    new ForgingBarEntry(new ItemDto(1285), 54, 1), // short sword
                    new ForgingBarEntry(new ItemDto(822, 10), 54, 1), // dart tips
                    new ForgingBarEntry(new ItemDto(4822, 15), 54, 1), // nails
                    new ForgingBarEntry(), // empty
                    new ForgingBarEntry(), // empty
                    new ForgingBarEntry(), // empty
                    new ForgingBarEntry(new ItemDto(42, 15), 55, 1), // arrowtips
                    new ForgingBarEntry(new ItemDto(1329), 55, 2), // scimitar
                    new ForgingBarEntry(new ItemDto(9427), 56, 1), // limbs
                    new ForgingBarEntry(new ItemDto(1299), 56, 2), // longsword
                    new ForgingBarEntry(new ItemDto(866, 5), 57, 1), // throwing knives
                    new ForgingBarEntry(new ItemDto(1159), 57, 2), // full helm
                    new ForgingBarEntry(new ItemDto(1181), 58, 2), // sq shield
                    new ForgingBarEntry(new ItemDto(9416), 59, 1), // grapple
                    new ForgingBarEntry(new ItemDto(1273), 55, 2), // pickaxe
                    new ForgingBarEntry(new ItemDto(1343), 59, 3), // warhammer
                    new ForgingBarEntry(new ItemDto(1369), 60, 3), // battleaxe
                    new ForgingBarEntry(new ItemDto(1109), 61, 3), // chainbody
                    new ForgingBarEntry(new ItemDto(1197), 62, 3), // kiteshield
                    new ForgingBarEntry(new ItemDto(3099), 63, 2), // claws
                    new ForgingBarEntry(new ItemDto(1315), 64, 3), // 2-handed sword
                    new ForgingBarEntry(new ItemDto(1085), 66, 3), // plateskirt
                    new ForgingBarEntry(new ItemDto(1071), 66, 3), // platelegs
                    new ForgingBarEntry(new ItemDto(1121), 68, 5) // platebody
                ])), // mithril bar
            new(2361,
                new SmeltingBarDefinition([new ItemDto(449), new ItemDto(453, 6)], 70, 37.5),
                new ForgingBarDefinition(62.0, [])), // adamantite bar
            new(2363,
                new SmeltingBarDefinition([new ItemDto(451), new ItemDto(453, 8)], 85, 50.0),
                new ForgingBarDefinition(75.0, [])), // rune bar
            new(21783, new SmeltingBarDefinition([new ItemDto(21779)], 80, 50.0), new ForgingBarDefinition()), // dragonbane bar
            new(21784, new SmeltingBarDefinition([new ItemDto(21780)], 80, 50.0), new ForgingBarDefinition()), // wallasalkibane bar
            new(21785, new SmeltingBarDefinition([new ItemDto(21781)], 80, 50.0), new ForgingBarDefinition()), // basiliskbane bar
            new(21786, new SmeltingBarDefinition([new ItemDto(21782)], 80, 50.0), new ForgingBarDefinition()) // abyssalbane bar
        ];

        /// <summary>
        ///     Smithes the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public static bool Smith(ICharacter character)
        {
            for (short i = 0; i < character.Inventory.Capacity; i++)
            {
                if (character.Inventory[i] == null)
                {
                    continue;
                }

                var barId = GetBarDefinitionID(character.Inventory[i]?.Id);
                if (barId != -1)
                {
                    return Smith(character, character.Inventory[i]);
                }
            }

            return false;
        }

        /// <summary>
        ///     Smithes the specified smith.
        /// </summary>
        /// <param name="character">The smither.</param>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static bool Smith(ICharacter character, IItem? item)
        {
            if (item == null)
            {
                return false;
            }

            if (!character.Inventory.Contains(2347))
            {
                character.SendChatMessage("You need a hammer in order to work with a " + item.Name.ToLower() + ".");
                return false;
            }

            var barId = GetBarDefinitionID(item.Id);
            if (barId == -1)
            {
                return false;
            }

            var definition = Bars[barId];
            var smithingInterfaceScript = character.ServiceProvider.GetRequiredService<SmithingInterface>();
            smithingInterfaceScript.Definition = definition;
            character.Widgets.OpenWidget(300, 0, smithingInterfaceScript, false);
            return true;
        }

        /// <summary>
        ///     Smelts the specified smelter.
        /// </summary>
        /// <param name="character">The character.</param>
        public static void Smelt(ICharacter character)
        {
            var products = new HashSet<int>();
            foreach (var t in Bars)
            {
                var hasResources = false;
                foreach (var ore in t.SmeltDefinition.RequiredOres)
                {
                    var resourceID = ore.Id;
                    var resourceCount = ore.Count;
                    hasResources = character.Inventory.Contains(resourceID, resourceCount);
                }

                if (hasResources)
                {
                    products.Add(t.BarID);
                }
            }

            if (products.Count <= 0)
            {
                character.SendChatMessage("You do not have any ores to smelt.");
                return;
            }

            var itemService = character.ServiceProvider.GetRequiredService<IItemService>();
            var dialogue = character.ServiceProvider.GetRequiredService<InteractiveDialogueScript>();
            dialogue.ProductIds = products.ToArray();
            dialogue.PerformMakeProductCallback = (itemID, count) =>
            {
                var barId = GetBarDefinitionID(itemID);
                if (barId == -1)
                {
                    return false;
                }

                var definition = Bars[barId];
                if (character.Statistics.GetSkillLevel(StatisticsConstants.Smithing) < definition.SmeltDefinition.RequiredSmithingLevel)
                {
                    character.SendChatMessage("You need a Smithing level of at least " + definition.SmeltDefinition.RequiredSmithingLevel + " to smelt " +
                                              itemService.FindItemDefinitionById(definition.BarID).Name.ToLower() + ".");
                    return false;
                }

                if (count <= 0)
                {
                    count = 1;
                }

                var task = character.ServiceProvider.GetRequiredService<SmeltTask>();
                task.Definition = definition;
                task.TotalSmeltCount = count;
                character.QueueTask(task);
                return true;
            };
            dialogue.Info = "How many bars you would like to smelt?<br>Choose a number, then click the bar to begin.";
            dialogue.SetMaxCount(character.Inventory.Capacity, false);
            dialogue.SetCurrentCount(character.Inventory.Capacity, false);
            InteractiveDialogueScript.OpenInteractiveDialogue(character, dialogue);
        }

        /// <summary>
        ///     Gets the bar definition identifier.
        /// </summary>
        /// <param name="barID">The bar item identifier.</param>
        /// <returns></returns>
        public static int GetBarDefinitionID(int? barID)
        {
            if (barID == null)
            {
                return -1;
            }

            for (var i = 0; i < Bars.Length; i++)
            {
                if (Bars[i].BarID == barID)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}