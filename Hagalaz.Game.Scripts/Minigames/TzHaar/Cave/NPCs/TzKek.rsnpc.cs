using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Model.Combat;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.NPCs
{
    [NpcScriptMetaData([2736, 2737])]
    public class TzKek : StandardCaveNpc
    {
        public TzKek(INpcBuilder npcBuilder) : base(npcBuilder) { }

        /// <summary>
        ///     Happens when attacker attack reaches the npc.
        ///     By default this method does nothing and returns the damage provided in parameters.
        /// </summary>
        /// <param name="attacker">Creature which performed attack.</param>
        /// <param name="damageType">Type of the attack.</param>
        /// <param name="damage">Amount of damage inflicted on this character or -1 if it's a miss.</param>
        /// <returns>
        ///     Return's amount of damage remains after defence.
        /// </returns>
        public override int OnAttack(ICreature attacker, DamageType damageType, int damage)
        {
            var soaked = -1;
            var dmg = attacker.Combat.Attack(Owner, DamageType.Reflected, 10, ref soaked);
            var splat = new HitSplat(Owner);
            splat.SetFirstSplat(HitSplatType.HitDeflectDamage, dmg == -1 ? 0 : dmg, false);
            attacker.QueueHitSplat(splat);
            return base.OnAttack(attacker, damageType, damage);
        }
    }
}