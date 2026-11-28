using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;

using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Armadyl
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([6223])]
    public class WingmanSkree : BodyGuard
    {
        private readonly INpcAttackService _attacks;
        private readonly IAviansieFlightRule _flight;

        public WingmanSkree(INpc owner, INpcAttackService attacks, IAviansieFlightRule flight, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
            _attacks = attacks;
            _flight = flight;
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Magic;

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance() => 7;

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MagicNormal;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            _attacks.PerformMagic(Owner, target, new NpcProjectile(1190, 25, FromHeight: 50), 250, baseDelay: 15);
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
            !_flight.TryDenyMelee(Owner, attacker, CanRetaliateTo(attacker)) && base.CanBeAttackedBy(attacker);
    }
}
