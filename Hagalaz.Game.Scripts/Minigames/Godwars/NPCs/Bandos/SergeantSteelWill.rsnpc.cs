using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos
{
    /// <summary>
    ///     Contains sergeant steel will script.
    /// </summary>
    [NpcScriptMetaData([6263])]
    public class SergeantSteelWill : BodyGuard
    {
        private readonly INpcAttackService _attacks;
        public SergeantSteelWill(INpc owner, INpcAttackService attacks, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
            _attacks = attacks;
        }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            // Magic attack

            RenderAttack();


            _attacks.PerformMagic(Owner, target, new NpcProjectile(1203, 50), 164);
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
    }
}
