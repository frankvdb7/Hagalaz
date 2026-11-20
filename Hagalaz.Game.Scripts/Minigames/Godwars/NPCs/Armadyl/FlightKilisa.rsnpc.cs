using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Armadyl
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([6227])]
    public class FlightKilisa : BodyGuard
    {
        public FlightKilisa(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
        }
        /// <summary>
        ///     Get's if this npc can be attacked by the specified character ('attacker').
        ///     By default , this method does check if this npc is attackable.
        ///     This method also checks if the attacker is a character, wether or not it
        ///     has the required slayer level.
        /// </summary>
        /// <param name="attacker">Creature which is attacking this npc.</param>
        /// <returns>
        ///     If attack can be performed.
        /// </returns>
        public override bool CanBeAttackedBy(ICreature attacker) =>
            !AviansieFlight.TryDenyMelee(Owner, attacker, CanRetaliateTo) && base.CanBeAttackedBy(attacker);

    }
}
