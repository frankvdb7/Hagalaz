using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Armadyl
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([6227])]
    public class FlightKilisa : BodyGuard
    {
        private readonly IAviansieFlightRule _flight;

        public FlightKilisa(INpc owner, IAviansieFlightRule flight, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator) => _flight = flight;
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
            !_flight.TryDenyMelee(Owner, attacker, CanRetaliateTo(attacker)) && base.CanBeAttackedBy(attacker);

    }
}
