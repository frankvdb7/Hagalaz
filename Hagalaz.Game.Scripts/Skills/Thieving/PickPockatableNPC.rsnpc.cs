using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Skills.Thieving
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([
        1, 2, 3, 4, 5, 6, 16, 24, 170,
        7, 1757, 1758, 1760,
        1715,
        1714, 1716,
        1710, 1711, 1712,
        15, 18,
        187, 2267, 2268, 2269, 8122,
        5752, 5753, 5754, 5755, 5756, 5757, 5758, 5759, 5760, 5761, 5762, 5763, 5764, 5765, 5766, 5767,
        2234, 2235,
        9, 32, 206, 296, 297, 298, 299, 344, 346, 368, 678, 812, 3228, 3229, 3230, 3231, 3407, 3408,
        2462,
        23, 26,
        1905,
        20, 2256,
        13195, 13212, 13213,
        66, 67, 68, 168, 169, 2249, 2250, 2251, 2371, 2649, 2650, 6002, 6004,
        21,
        2109, 2110, 2111, 2112, 2113, 2114, 2115, 2116, 2117, 2118, 2119, 2120, 2121, 2122, 2123, 2124, 2125, 2126
    ])]
    public class PickPockatableNpc : NpcScriptBase
    {
        public PickPockatableNpc(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
            _definition = Thieving.Ppd.First(def => def.NpcIDs.Contains(owner.Definition.Id));
        }
        /// <summary>
        ///     The pickpocket definition.
        /// </summary>
        private PickPocketDefinition _definition;

        /// <summary>
        ///     Happens when character clicks NPC and then walks to it and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        ///     handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option3Click)
            {
                clicker.Interrupt(this);
                clicker.QueueTask(() => Thieving.PickPocket(clicker, Owner, _definition));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

    }
}
