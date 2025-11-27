using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Thieving
{
    /// <summary>
    ///     A static class for thieving.
    /// </summary>
    public static class Thieving
    {
        /// <summary>
        ///     The stun animation.
        /// </summary>
        public const short StunAnimation = 422;

        /// <summary>
        ///     The stunned animation
        /// </summary>
        public const short StunnedAnimation = 424;

        /// <summary>
        ///     The pick pocketing animation.
        /// </summary>
        public const short PickPocketingAnimation = 881;

        /// <summary>
        ///     The double loot animation.
        /// </summary>
        public const short DoubleLootAnimation = 5074;

        /// <summary>
        ///     The double loot graphic.
        /// </summary>
        public const short DoubleLootGraphic = 873;

        /// <summary>
        ///     The triple loot animation.
        /// </summary>
        public const short TripleLootAnimation = 5075;

        /// <summary>
        ///     The triple loot graphic.
        /// </summary>
        public const short TripleLootGraphic = 874;

        /// <summary>
        ///     The quadruple animation.
        /// </summary>
        public const short QuadrupleLootAnimation = 5078;

        /// <summary>
        ///     The quadruple loot graphic.
        /// </summary>
        public const short QuadrupleLootGraphic = 875;

        /// <summary>
        ///     The PickPocketDefinitons.
        /// </summary>
        public static readonly PickPocketDefinition[] Ppd =
        [
            // npcIDs, level, experience, faildamage, extralootthievinglevel, extralootagilitylevel 
            // ---
            // if the character has the required thieving and agility extralootlevel he will earn; double loot 
            // if character has 10 levels above both the levels, he will earn; triple loot
            // if character has 20 levels above both the levels, he will earn; quadruple loot
            // ---
            // for example, Man is id 1, needs thieving level 1, gives 8 experience, on fail hits 10 damage, 
            // on level 11 or higher thieivng and 1 or higher agility, the above formula counts.
            new([1, 2, 3, 4, 5, 6, 16, 24, 170], 1, 8.0, 10, 11, 1), // man
            new([7, 1757, 1758, 1760], 10, 14.5, 10, 20, 10), // farmer
            new([1715], 15, 18.5, 10, 25, 15), // female h.a.m. member
            new([1714, 1716], 20, 22.2, 20, 30, 20), // male h.a.m. member
            new([1710, 1711, 1712], 20, 22.5, 30, 30, 20), // h.a.m. guard
            new([15, 18], 25, 26.0, 20, 35, 25), // al-kharid warrior / woman
            new([187, 2267, 2268, 2269, 8122], 32, 36.5, 20, 42, 32), // rogue
            new([5752, 5753, 5754, 5755, 5756, 5757, 5758, 5759, 5760, 5761, 5762, 5763, 5764, 5765, 5766, 5767, 5768], 36, 40.0, 10, 46, 36), // cave goblin
            new([2234, 2235], 38, 43.0, 30, 48, 38), // master farmer
            new([9, 32, 206, 296, 297, 298, 299, 344, 346, 368, 678, 812, 3228, 3229, 3230, 3231, 3407, 3408], 40, 46.8, 20, 50, 40), // guard
            new([2462], 45, 65.0, 20, 55, 45), // fremennik citizen
            new([23, 26], 55, 84.3, 30, 65, 55), // ardougne knight
            new([1905], 65, 137.5, 50, 75, 65), // menaphite thug
            new([20, 2256], 70, 151.8, 30, 80, 70), // paladin
            new([13195, 13212, 13213], 70, 150.0, 60, byte.MaxValue, byte.MaxValue), // monkey knife fighter
            new([66, 67, 68, 168, 169, 2249, 2250, 2251, 2371, 2649, 2650, 6002, 6004], 75, 198.3, 10, 85, 75), // gnome
            new([21], 80, 273.3, 40, 90, 80), // hero
            new([2109, 2110, 2111, 2112, 2113, 2114, 2115, 2116, 2117, 2118, 2119, 2120, 2121, 2122, 2123, 2124, 2125, 2126], 90, 556.5, 10, byte.MaxValue, byte.MaxValue) // dwarf trader
        ];

        /// <summary>
        ///     The StealDefinition.
        /// </summary>
        public static readonly StealDefinition[] Sd =
        [
            new([4706, 4708], 2, 10.0, 2, -1, 634), // vegetable stall
            new([34384], 5, 16.0, 4, -1), // bakers stal
            new([], 5, 16.0, 17, -1), // general stall
            new([], 5, 16.0, 17, -1), // crafting stall
            new([], 5, 16.0, 17, -1), // monkey food stall
            new([], 5, 16.0, 15, -1), // tea stall
            new([], 15, 10.0, 17, -1), // rock cake stall
            new([34383], 20, 24.0, 8, -1), // silk stall
            new([14011], 22, 27.0, 25, -1), // wine stall
            new([], 27, 10.0, 25, -1, 2047), // seed stall
            new([34387], 35, 36.0, 25, -1), // fur stall
            new([], 42, 42.0, 25, -1), // fish stall
            new([], 49, 52.0, 28, -1), // crossbow stall
            new([34382], 50, 54.0, 50, -1), // silver stall
            new([34386], 65, 81.3, 133, -1), // spice stall
            new([], 65, 100.0, 100, -1), // magic stall
            new([], 65, 160.0, 100, -1), // scimitar stall
            new([], 75, 160.0, 300, -1), // gem stall

            new([4111], 1, 10.0, 5, -1, -1), // Bronze Chest            
            new([4116], 20, 20.0, 5, -1, -1), // Steel Chest            
            new([4121], 40, 40.0, 5, -1, -1), // Black Chest
            new([4126], 60, 65.0, 5, -1, -1), // Silver Chest
            new([59731], 80, 130.0, 5, -1, -1) // Gold Chest
        ];

        /// <summary>
        ///     Steals the specified clicker.
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="stall">The stall.</param>
        public static async Task Steal(ICharacter clicker, IGameObject obj, Stall stall)
        {
            if (clicker.HasState<ThievingStallState>())
            {
                return;
            }
            // check if the object isn't empty.
            var checkObj = obj.Region.FindStandardGameObject(obj.Location.RegionLocalX, obj.Location.RegionLocalY, obj.Location.Z);
            if (checkObj == null || checkObj.Id != obj.Id)
            {
                return;
            }

            var lootService = clicker.ServiceProvider.GetRequiredService<ILootService>();
            var lootTable = await lootService.FindGameObjectLootTable(obj.Definition.LootTableId);
            if (lootTable == null)
            {
                return;
            }

            if (clicker.Statistics.GetSkillLevel(StatisticsConstants.Thieving) < stall.Definition.RequiredLevel)
            {
                clicker.SendChatMessage("You need a thieving level of " + stall.Definition.RequiredLevel + " in order to steal from this stall!");
                return;
            }

            var maximumLootCount = lootTable.MaxResultCount;
            if (clicker.Inventory.FreeSlots < maximumLootCount)
            {
                clicker.SendChatMessage(GameStrings.InventoryFull);
                return;
            }

            clicker.AddState(new ThievingStallState());
            clicker.QueueTask(new StandardStealTask(clicker, obj, stall, 2));
        }

        /// <summary>
        ///     Picks the pocket.
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="npc">The NPC.</param>
        /// <param name="definition">The definition.</param>
        public static async Task PickPocket(ICharacter clicker, INpc npc, PickPocketDefinition definition)
        {
            if (clicker.HasState<ThievingNpcState>())
            {
                return;
            }

            var lootService = clicker.ServiceProvider.GetRequiredService<ILootService>();
            var lootTable = await lootService.FindGameObjectLootTable(npc.Definition.PickPocketingLootTableId);
            if (lootTable == null)
            {
                return;
            }

            if (clicker.Statistics.GetSkillLevel(StatisticsConstants.Thieving) < definition.RequiredLevel)
            {
                clicker.SendChatMessage("You need a thieving level of " + definition.RequiredLevel + " in order to pickpocket this npc!");
                return;
            }

            var maximumLootCount = lootTable.MaxResultCount;
            /*lock (clicker.Inventory.GetLock())
			{
				if (clicker.Inventory.FreeSlots < maximumLootCount)
				{
					clicker.SendMessage(GameMessages.INVENTORY_FULL);
					return;
				}
			}*/
            clicker.AddState(new ThievingNpcState());
            clicker.QueueTask(new StandardPickPocketTask(clicker, npc, definition, maximumLootCount, 2));
        }
    }
}