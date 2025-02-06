using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model.Combat;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.NPCs
{
    [NpcScriptMetaData([2741, 2742])]
    public class YtMejKot : StandardCaveNpc
    {
        public YtMejKot(INpcBuilder npcBuilder) : base(npcBuilder) { }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            var preDmg = target.Combat.IncomingAttack(Owner, DamageType.StandardMelee, ((INpcCombat)Owner.Combat).GetMeleeDamage(target), 0);
            var soaked = -1;
            var damage = target.Combat.Attack(Owner, DamageType.StandardMelee, preDmg, ref soaked);
            var splat = new HitSplat(Owner);
            splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target) <= damage);
            if (soaked != -1)
            {
                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
            }

            target.QueueHitSplat(splat);

            if (damage <= 0)
            {
                return;
            }

            if (target is not ICharacter character)
            {
                return;
            }

            if (character.Statistics.GetMaximumLifePoints() / 2 >= character.Statistics.LifePoints)
            {
                var npcs = Owner.Viewport.VisibleCreatures.OfType<INpc>().Where(n => Owner.WithinRange(n, 1));
                foreach (var npc in npcs)
                {
                    HealNpc(npc);
                }

                HealNpc(Owner);
            }
        }

        /// <summary>
        ///     Heals the NPC.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        private static void HealNpc(INpc npc) => npc.Statistics.HealLifePoints(RandomStatic.Generator.Next(1, 100));
    }
}